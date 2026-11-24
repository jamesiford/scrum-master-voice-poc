# Builds the Teams app package for this deployment and uploads it to the organisation's app catalogue.
#   pwsh infra/hooks/teams-app.ps1               build the package; upload only if a token is available (azd postdeploy)
#   pwsh infra/hooks/teams-app.ps1 -Interactive  also sign in with a device code and upload (one person, once)
# The package uses the deployment's bot id (the user-assigned identity client id) in the manifest id and bots[0].botId.
# HUMAN GATE (docs/DEPLOY.md): Microsoft Graph accepts catalogue uploads only for a signed-in user with AppCatalog.Submit, so the upload
# needs one interactive sign-in by a Teams administrator or a user allowed to upload custom apps.
param([switch]$Interactive)
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$botId = $env:BOT_APP_ID
if (-not $botId) { throw 'BOT_APP_ID is not set; run azd provision first.' }
$src = Join-Path $PSScriptRoot '../../appPackage'
$dist = Join-Path $src 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

$manifest = Get-Content (Join-Path $src 'manifest.json') -Raw | ConvertFrom-Json
$manifest.id = $botId
$manifest.bots[0].botId = $botId
if ($env:TEAMS_APP_VERSION) { $manifest.version = $env:TEAMS_APP_VERSION }
$stage = Join-Path ([IO.Path]::GetTempPath()) "zava-teams-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $stage | Out-Null
($manifest | ConvertTo-Json -Depth 20) | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8
Copy-Item (Join-Path $src 'color.png'), (Join-Path $src 'outline.png') $stage
$zip = Join-Path $dist "zava-scrum-agent-$($env:AZURE_ENV_NAME)-$($manifest.version).zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item $stage -Recurse -Force
Write-Check PASS 'Teams app package' "$zip (id $botId, version $($manifest.version))"

function Get-CatalogToken {
  # Microsoft Graph Command Line Tools, a first-party public client that is pre-authorised in every tenant.
  $client = '14d82eec-204b-4c2f-b7e8-296a70dab67e'
  $scope = 'https://graph.microsoft.com/AppCatalog.Submit offline_access'
  $dc = Invoke-RestMethod -Method Post "https://login.microsoftonline.com/$($env:AZURE_TENANT_ID)/oauth2/v2.0/devicecode" -Body @{ client_id = $client; scope = $scope }
  Write-Host "`n$($dc.message)`n" -ForegroundColor Yellow
  $deadline = (Get-Date).AddSeconds($dc.expires_in)
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $dc.interval
    try { return (Invoke-RestMethod -Method Post "https://login.microsoftonline.com/$($env:AZURE_TENANT_ID)/oauth2/v2.0/token" -Body @{ grant_type = 'urn:ietf:params:oauth:grant-type:device_code'; client_id = $client; device_code = $dc.device_code }).access_token }
    catch { if ($_.ErrorDetails.Message -notmatch 'authorization_pending') { throw } }
  }
  throw 'The device code expired.'
}

if (-not $Interactive) {
  Write-Check GATE 'Teams app upload' "A Teams administrator must upload $zip once: run 'pwsh infra/hooks/teams-app.ps1 -Interactive', or Teams admin centre > Teams apps > Manage apps > Upload new app."
  return
}

$token = Get-CatalogToken
$h = @{ Authorization = "Bearer $token" }
$existing = (Invoke-RestMethod "https://graph.microsoft.com/v1.0/appCatalogs/teamsApps?`$filter=externalId eq '$botId'" -Headers $h).value | Select-Object -First 1
$uri = if ($existing) { "https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/$($existing.id)/appDefinitions" } else { 'https://graph.microsoft.com/v1.0/appCatalogs/teamsApps?requiresReview=false' }
$bytes = [IO.File]::ReadAllBytes($zip)
$r = Invoke-RestMethod -Method Post -Uri $uri -Headers $h -ContentType 'application/zip' -Body $bytes
Write-Check PASS 'Teams app upload' "$(if ($existing) { 'new version of' } else { 'published' }) catalogue app $($r.id)"
