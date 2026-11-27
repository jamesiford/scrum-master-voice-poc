# Opt-in postdeploy hook. Provisioning and the public certificate gate precede selecting GraphMedia (ADR 0023).
param([switch]$ConsentOnly, [switch]$RuntimeOnly)
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment
if ($env:MEDIA_HOST_ENABLED -ne 'true') { Write-Check SKIP 'Windows media host' 'MEDIA_HOST_ENABLED is false'; return }
Assert-MediaDeploymentEnvironment
if ($ConsentOnly -and $RuntimeOnly) { throw 'ConsentOnly and RuntimeOnly cannot be combined.' }
$displayName = "zava-media-$($env:AZURE_ENV_NAME)"
$app = (Invoke-Graph GET "/applications?`$filter=displayName eq '$displayName'").value
if (@($app).Count -gt 1) { throw "More than one calling application is named $displayName." }
if (-not $app) {
  if ($RuntimeOnly) { throw 'RuntimeOnly requires the existing dedicated calling application.' }
  $app = Invoke-Graph POST '/applications' @{ displayName = $displayName; signInAudience = 'AzureADMyOrg' }
}
else { $app = @($app)[0] }
Set-AzdValue MEDIA_APP_ID $app.appId
Set-AzdValue MEDIA_APP_OBJECT_ID $app.id
$sp = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '$($app.appId)'").value
if (-not $sp) {
  if ($RuntimeOnly) { throw 'RuntimeOnly requires the existing calling service principal.' }
  $sp = Invoke-Graph POST '/servicePrincipals' @{ appId = $app.appId }
}
else { $sp = @($sp)[0] }
$graph = (Invoke-Graph GET "/servicePrincipals?`$filter=appId eq '00000003-0000-0000-c000-000000000000'&`$select=id,appRoles").value[0]
$role = $graph.appRoles | Where-Object { $_.value -eq 'Calls.AccessMedia.All' -and $_.allowedMemberTypes -contains 'Application' }
if (-not $role) { throw 'Graph did not expose Calls.AccessMedia.All.' }
if (-not $RuntimeOnly) { Invoke-Graph PATCH "/applications/$($app.id)" @{
  requiredResourceAccess = @(@{ resourceAppId = '00000003-0000-0000-c000-000000000000'; resourceAccess = @(@{ id = $role.id; type = 'Role' }) })
} | Out-Null }
$grants = (Invoke-Graph GET "/servicePrincipals/$($sp.id)/appRoleAssignments").value
if (-not ($grants | Where-Object { $_.appRoleId -eq $role.id -and $_.resourceId -eq $graph.id })) {
  if ($RuntimeOnly) { throw 'RuntimeOnly requires the existing Calls.AccessMedia.All consent.' }
  Invoke-Graph POST "/servicePrincipals/$($sp.id)/appRoleAssignments" @{ principalId = $sp.id; resourceId = $graph.id; appRoleId = $role.id } | Out-Null
}
Write-Check PASS 'media app Graph permission' 'Calls.AccessMedia.All; join consent is per-meeting RSC, not a tenant-wide join role.'
if ($ConsentOnly) { return }
foreach ($name in @('MEDIA_VM_NAME', 'MEDIA_FQDN', 'MEDIA_PUBLIC_IP', 'MEDIA_VAULT_NAME', 'MEDIA_STORAGE_NAME', 'API_URI')) {
  if (-not [Environment]::GetEnvironmentVariable($name)) { throw "$name is missing; provision the opt-in Bicep module first." }
}
if (-not $RuntimeOnly) {
az deployment group create -g $env:AZURE_RESOURCE_GROUP --name media-bot-registration `
  --template-file "$PSScriptRoot\..\modules\media-bot-registration.bicep" `
  --parameters "appId=$($app.appId)" "tenantId=$($env:AZURE_TENANT_ID)" "botName=bot-media-$($env:AZURE_ENV_NAME)" "fqdn=$($env:MEDIA_FQDN)" -o none
if ($LASTEXITCODE -ne 0) { throw 'Cannot register the Azure-managed calling bot and Teams calling webhook.' }
}

$certificate = az keyvault certificate show --vault-name $env:MEDIA_VAULT_NAME --name media-tls -o json 2>&1
$issueCertificate = $false
if ($LASTEXITCODE -ne 0) {
  if (($certificate -join "`n") -notmatch 'CertificateNotFound') {
    throw 'Cannot read media-tls. Check the vault certificate role and RBAC propagation before rerunning the hook.'
  }
  $issueCertificate = $true
}
else {
  $certificateInfo = $certificate | ConvertFrom-Json
  $expires = $certificateInfo.attributes.expires
  $issueCertificate = [DateTimeOffset]$expires -le [DateTimeOffset]::UtcNow.AddDays(30)
  if ($RuntimeOnly) {
    # Graph's Binary identifier is returned as hex by this tenant and as base64 by other clients.
    $thumbprintHex = $certificateInfo.x509ThumbprintHex
    $thumbprintBase64 = [Convert]::ToBase64String([Convert]::FromHexString($thumbprintHex))
    $registered = (Invoke-Graph GET "/applications/$($app.id)?`$select=keyCredentials").keyCredentials |
      Where-Object { $_.displayName -eq 'media-tls' -and ($_.customKeyIdentifier -eq $thumbprintHex -or $_.customKeyIdentifier -eq $thumbprintBase64) }
    if (-not $registered) { throw 'RuntimeOnly requires the current vault certificate to be registered before stopping the host.' }
  }
}
if ($issueCertificate) {
  if ($RuntimeOnly) { throw 'RuntimeOnly cannot renew/register certificates; run the full authorised certificate hook first.' }
  $issued = az vm run-command invoke -g $env:AZURE_RESOURCE_GROUP -n $env:MEDIA_VM_NAME --command-id RunPowerShellScript `
    --scripts "@$PSScriptRoot\media-certificate.ps1" --parameters "Fqdn=$($env:MEDIA_FQDN)" "VaultName=$($env:MEDIA_VAULT_NAME)" -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0 -or ($issued.value.message -join "`n") -notmatch 'MEDIA_TLS_IMPORTED:') {
    throw "Automated MEDIA-TLS issuance/import failed. Inspect the VM run-command result; see docs/DEPLOY.md."
  }
  Write-Check PASS 'MEDIA-TLS' 'HTTP-01 certificate imported directly to Key Vault by the VM identity.'
}
$work = Join-Path ([IO.Path]::GetTempPath()) ('zava-media-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  $build = Join-Path $work 'publish'
  dotnet publish "$PSScriptRoot\..\..\src\Zava.Scrum.MediaBot\Zava.Scrum.MediaBot.csproj" -c Release -r win-x64 --self-contained true -o $build --verbosity quiet
  if ($LASTEXITCODE -ne 0) { throw 'Media host publish failed.' }
  if (-not (Test-Path (Join-Path $build 'NativeMedia.dll'))) {
    throw 'The media publish is missing the SDK native payload. Check the aligned Microsoft.Skype.Bots.Media reference.'
  }
  if (-not (Get-ChildItem -Path $build -Filter 'libSkiaSharp.dll' -Recurse)) {
    throw 'The media publish is missing the native SkiaSharp renderer payload.'
  }
  $zip = Join-Path $work 'media.zip'
  Compress-Archive -Path "$build\*" -DestinationPath $zip
  $hash = (Get-FileHash $zip -Algorithm SHA256).Hash
  $blob = "$hash.zip"
  az storage blob upload --account-name $env:MEDIA_STORAGE_NAME --container-name media-packages --name $blob --file $zip --auth-mode login --overwrite -o none
  if ($LASTEXITCODE -ne 0) { throw 'Cannot upload the media package with Entra authentication.' }
  $secretUrl = "https://management.azure.com/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$($env:AZURE_RESOURCE_GROUP)/providers/Microsoft.App/containerApps/$($env:API_APP_NAME)/listSecrets?api-version=2024-03-01"
  $secrets = az rest --method POST --url $secretUrl -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw 'Cannot read the existing bridge control secret.' }
  $key = ($secrets.value | Where-Object { $_.name -eq 'bridge-key' }).value
  if (-not $key) { throw 'The API has no bridge-key secret.' }
  $secretFile = Join-Path $work 'control-key.txt'
  [IO.File]::WriteAllText($secretFile, $key)
  az keyvault secret set --vault-name $env:MEDIA_VAULT_NAME --name media-control-key --file $secretFile -o none
  if ($LASTEXITCODE -ne 0) { throw 'Cannot deliver the control key to the media vault.' }
  Remove-Item -LiteralPath $secretFile
  $key = $null
  if (-not $env:APPLICATIONINSIGHTS_CONNECTION_STRING) { $env:APPLICATIONINSIGHTS_CONNECTION_STRING = Get-InsightsConnectionString }
  $insightsFile = Join-Path $work 'insights.txt'
  [IO.File]::WriteAllText($insightsFile, $env:APPLICATIONINSIGHTS_CONNECTION_STRING)
  az keyvault secret set --vault-name $env:MEDIA_VAULT_NAME --name media-insights --file $insightsFile -o none
  if ($LASTEXITCODE -ne 0) { throw 'Cannot deliver the existing Insights connection to the media vault.' }
  Remove-Item -LiteralPath $insightsFile
  $result = az vm run-command invoke -g $env:AZURE_RESOURCE_GROUP -n $env:MEDIA_VM_NAME --command-id RunPowerShellScript `
    --scripts "@$PSScriptRoot\media-bootstrap.ps1" --parameters "StorageName=$($env:MEDIA_STORAGE_NAME)" "PackageName=$blob" `
      "PackageSha256=$hash" "VaultName=$($env:MEDIA_VAULT_NAME)" "AppId=$($app.appId)" "TenantId=$($env:AZURE_TENANT_ID)" `
      "Fqdn=$($env:MEDIA_FQDN)" "PublicIp=$($env:MEDIA_PUBLIC_IP)" "ApiBase=$($env:API_URI)" -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw 'The Windows media bootstrap command failed.' }
  $text = ($result.value.message -join "`n")
  if ($text -notmatch 'PUBLIC_CERT:([A-Za-z0-9+/=]+)') { throw "Bootstrap did not return a public certificate. Inspect VM run-command status; no secrets are printed here." }
  $public = $Matches[1]
  $cert = [Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadCertificate([Convert]::FromBase64String($public))
  $old = (Invoke-Graph GET "/applications/$($app.id)?`$select=keyCredentials").keyCredentials | Where-Object { $_.displayName -ne 'media-tls' }
  $keys = @($old) + @(@{
    type = 'AsymmetricX509Cert'; usage = 'Verify'; key = $public; displayName = 'media-tls'
    startDateTime = $cert.NotBefore.ToUniversalTime().ToString('o'); endDateTime = $cert.NotAfter.ToUniversalTime().ToString('o')
  })
  if ($RuntimeOnly) {
    $thumbprintHex = $cert.Thumbprint
    $thumbprintBase64 = [Convert]::ToBase64String($cert.GetCertHash())
    $registered = (Invoke-Graph GET "/applications/$($app.id)?`$select=keyCredentials").keyCredentials |
      Where-Object { $_.displayName -eq 'media-tls' -and ($_.customKeyIdentifier -eq $thumbprintHex -or $_.customKeyIdentifier -eq $thumbprintBase64) }
    if (-not $registered) { throw 'RuntimeOnly refuses to start an unregistered certificate.' }
  }
  else { Invoke-Graph PATCH "/applications/$($app.id)" @{ keyCredentials = $keys } | Out-Null }
  $started = az vm run-command invoke -g $env:AZURE_RESOURCE_GROUP -n $env:MEDIA_VM_NAME --command-id RunPowerShellScript `
    --scripts "`$ErrorActionPreference = 'Stop'; Start-ScheduledTask -TaskName 'ZavaMediaBot'; Write-Output 'MEDIA_TASK_STARTED'" -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0 -or ($started.value.message -join "`n") -notmatch 'MEDIA_TASK_STARTED') {
    throw 'Cannot start the media task after registering its Graph certificate.'
  }
  $manifest = Get-Content "$PSScriptRoot\..\..\appPackage\media-bot\manifest.template.json" -Raw
  $manifest.Replace('{{MEDIA_APP_ID}}', $app.appId).Replace('{{MEDIA_FQDN}}', $env:MEDIA_FQDN) | Set-Content (Join-Path $work 'manifest.json') -Encoding utf8
  Copy-Item "$PSScriptRoot\..\..\appPackage\color.png", "$PSScriptRoot\..\..\appPackage\outline.png" -Destination $work
  $package = "$PSScriptRoot\..\..\appPackage\dist\zava-media-bot.zip"
  New-Item -ItemType Directory -Path (Split-Path $package) -Force | Out-Null
  Compress-Archive -Path (Join-Path $work 'manifest.json'), (Join-Path $work 'color.png'), (Join-Path $work 'outline.png') -DestinationPath $package -Force
  $health = Invoke-RestMethod "https://$($env:MEDIA_FQDN)/healthz" -TimeoutSec 30
  if ($health.service -ne 'media-bot' -or $health.recording -ne $false -or $health.maxConcurrentMeetings -ne 1 -or $health.boardRendererReady -ne $true) { throw 'The media host did not pass HTTPS and native-renderer readiness with the one-meeting cap.' }
  Write-Check PASS 'Windows media host ready' "https://$($env:MEDIA_FQDN); concurrent call cap 1; no recording."
  az containerapp update -g $env:AZURE_RESOURCE_GROUP -n $env:API_APP_NAME `
    --set-env-vars "Bridge__GraphMediaBaseUrl=https://$($env:MEDIA_FQDN)" -o none
  if ($LASTEXITCODE -ne 0) { throw 'Cannot configure the ready native media origin on the existing API.' }
  Write-Check GATE 'MEDIA-RSC' "A meeting organiser must install appPackage/dist/zava-media-bot.zip in each meeting chat and consent to Calls.JoinGroupCalls.Chat. See docs/DEPLOY.md."
}
finally {
  # This is the unique temporary deployment artefact directory created above, never the workspace.
  Remove-Item -LiteralPath $work -Recurse -Force
}
