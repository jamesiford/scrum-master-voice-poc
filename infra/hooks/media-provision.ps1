# Existing-environment provisioning avoids resetting working Container Apps to the greenfield placeholder image.
param([switch]$ValidateOnly)
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3' -or $env:MEDIA_HOST_ENABLED -ne 'true') {
  throw 'Opt-in media provisioning is authorised only for zavaw3/westus3.'
}
if ($env:API_APP_NAME -notmatch '^ca-api-([a-z0-9]{13})$') { throw 'Cannot resolve the existing environment resource suffix.' }
$token = $Matches[1]
$parameters = @{
  location = @{ value = $env:AZURE_LOCATION }
  token = @{ value = $token }
  adminObjectId = @{ value = $env:ADMIN_OBJECT_ID }
  vmSize = @{ value = $(if ($env:MEDIA_VM_SIZE) { $env:MEDIA_VM_SIZE } else { 'Standard_D4s_v5' }) }
  serviceFqdn = @{ value = $(if ($env:MEDIA_SERVICE_FQDN) { $env:MEDIA_SERVICE_FQDN } else { '' }) }
  tags = @{ value = @{ 'azd-env-name' = $env:AZURE_ENV_NAME; SecurityControl = 'Ignore'; owner = $env:ADMIN_UPN } }
}
$file = New-TemporaryFile
try {
  @{ parameters = $parameters } | ConvertTo-Json -Depth 8 | Set-Content $file -Encoding utf8
  $operation = if ($ValidateOnly) { 'validate' } else { 'create' }
  $result = az deployment group $operation -g $env:AZURE_RESOURCE_GROUP --name media-host `
    --template-file "$PSScriptRoot\..\modules\media-host.bicep" --parameters "@$file" -o json | ConvertFrom-Json
  if ($LASTEXITCODE -ne 0) { throw "Scoped media $operation failed." }
  if ($ValidateOnly) { Write-Check PASS 'Scoped media ARM validation'; return }
  $outputs = $result.properties.outputs
  foreach ($mapping in @(
    @('MEDIA_VM_NAME', 'vmName'), @('MEDIA_PUBLIC_IP', 'publicIpAddress'), @('MEDIA_FQDN', 'fqdn'),
    @('MEDIA_VAULT_NAME', 'vaultName'), @('MEDIA_STORAGE_NAME', 'storageName')
  )) {
    Set-AzdValue $mapping[0] $outputs.($mapping[1]).value
  }
  Set-AzdValue MEDIA_SERVICE_FQDN $outputs.fqdn.value
  Write-Check PASS 'Scoped media provision' 'Existing API, ACS bridge and their secrets were not reprovisioned.'
}
finally { Remove-Item -LiteralPath $file }
