# Publish only the two static legal pages without rotating Easy Auth credentials or changing protected application routes.
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if (-not $env:API_APP_NAME -or -not $env:AZURE_RESOURCE_GROUP) { throw 'Provision the API before publishing legal pages.' }
$url = "https://management.azure.com/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$($env:AZURE_RESOURCE_GROUP)/providers/Microsoft.App/containerApps/$($env:API_APP_NAME)/authConfigs/current?api-version=2024-03-01"
$cfg = az rest --method GET --url $url -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Cannot read API Easy Auth configuration.' }
$missing = @('/privacy.html', '/terms.html' | Where-Object { $_ -notin $cfg.properties.globalValidation.excludedPaths })
if ($missing.Count) {
  $cfg.properties.globalValidation.excludedPaths = @($cfg.properties.globalValidation.excludedPaths) + $missing
  $file = New-TemporaryFile
  try {
    @{ properties = $cfg.properties } | ConvertTo-Json -Depth 20 | Set-Content $file -Encoding utf8
    az rest --method PUT --url $url --body "@$file" -o none
    if ($LASTEXITCODE -ne 0) { throw 'Cannot publish API legal pages through Easy Auth.' }
  }
  finally { Remove-Item -LiteralPath $file }
}
Write-Check PASS 'Public legal pages' 'Only /privacy.html and /terms.html excluded; existing secrets and protected routes retained.'
