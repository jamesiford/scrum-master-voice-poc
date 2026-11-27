# Scoped update of the existing environment: workbook plus exporter settings, never images, identities, secrets or adapter defaults.
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3' -or $env:AZURE_SUBSCRIPTION_ID -ne '4b0dd31f-5379-4acc-8dcd-60f2929d2513') {
  throw 'This conductor observability update is authorised only for zavaw3/westus3 in the designated subscription.'
}
$connection = Get-InsightsConnectionString
foreach ($app in @($env:API_APP_NAME, $env:BRIDGE_APP_NAME)) {
  if (-not $app) { throw 'The existing API and bridge names are required.' }
  az containerapp update --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP -n $app `
    --set-env-vars "APPLICATIONINSIGHTS_CONNECTION_STRING=$connection" 'Telemetry__CaptureContent=false' -o none
  if ($LASTEXITCODE -ne 0) { throw "Cannot update the exporter settings on $app." }
}
$connection = $null
$parameterFile = Join-Path ([IO.Path]::GetTempPath()) ("zava-workbook-$([Guid]::NewGuid().ToString('N')).json")
try {
  @{
    '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters = @{
      location = @{ value = $env:AZURE_LOCATION }
      appInsightsName = @{ value = $env:APPINSIGHTS_NAME }
      tags = @{ value = @{ 'azd-env-name' = 'zavaw3' } }
    }
  } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $parameterFile -Encoding utf8
  az deployment group create --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP `
    --name observability-workbook --template-file "$PSScriptRoot\..\modules\workbook.bicep" `
    --parameters "@$parameterFile" -o none
  if ($LASTEXITCODE -ne 0) { throw 'The scoped workbook deployment failed.' }
}
finally { Remove-Item -LiteralPath $parameterFile -Force }
Write-Check PASS 'observability' 'Existing API/bridge exporters content-off; shared metadata workbook deployed from Bicep.'
