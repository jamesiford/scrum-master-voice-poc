# Additive storage rollout only; full greenfield provisioning uses the same module from data.bicep.
param(
  [ValidateSet('Export', 'Provision', 'Load', 'Complete')][string]$Mode,
  [string]$Output
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_RESOURCE_GROUP -ne 'rg-zava-scrum-zavaw3' -or $env:AZURE_LOCATION -ne 'westus3') {
  throw 'This additive existing-data rollout is restricted to zavaw3/westus3.'
}
$account = $env:COSMOS_ACCOUNT_NAME
$group = $env:AZURE_RESOURCE_GROUP
$database = 'zavascrum'
$endpoint = "https://$account.documents.azure.com:443/"
$principal = az ad signed-in-user show --query id -o tsv
if ($LASTEXITCODE -ne 0 -or -not $principal) { throw 'Cannot resolve the signed-in export/probe principal.' }
$reader = '88a3a8a5-54bd-5d88-98ca-b42d0e1a4284'
$writer = 'c78da66e-90d5-59c1-82da-17261000d721'
if ($Mode -in @('Export', 'Load') -and -not $Output) { throw 'An untracked output directory is required.' }
switch ($Mode) {
  'Export' {
    az cosmosdb sql role assignment create -g $group -a $account --role-definition-id 00000000-0000-0000-0000-000000000001 `
      --principal-id $principal --scope / --role-assignment-id $reader --output none
    if ($LASTEXITCODE -ne 0) { throw 'Read-only Cosmos export grant failed; no migration is permitted.' }
    dotnet run --project "$PSScriptRoot\..\..\tools\StorageProbe" -- export $endpoint $database $Output
    if ($LASTEXITCODE -ne 0) { throw 'Read-only export failed; no migration is permitted.' }
  }
  'Provision' {
    $identity = az identity list -g $group --query '[0].principalId' -o tsv
    if ($LASTEXITCODE -ne 0 -or -not $identity) { throw 'Cannot resolve the existing application identity.' }
    az deployment group create -g $group --name r2-storage-additive --template-file "$PSScriptRoot\..\modules\team-records.bicep" `
      --parameters cosmosAccountName=$account databaseName=$database identityPrincipalId=$identity --output none
    if ($LASTEXITCODE -ne 0) { throw 'Additive TeamRecords provisioning failed.' }
  }
  'Load' {
    az cosmosdb sql role assignment create -g $group -a $account --role-definition-id 00000000-0000-0000-0000-000000000002 `
      --principal-id $principal --scope "/dbs/$database/colls/TeamRecords" --role-assignment-id $writer --output none
    if ($LASTEXITCODE -ne 0) { throw 'Container-scoped synthetic load grant failed.' }
    dotnet run --project "$PSScriptRoot\..\..\tools\StorageProbe" -- load $endpoint $database $Output
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic storage load failed; retain measured error, do not claim no 413.' }
  }
  'Complete' {
    foreach ($id in @($reader, $writer)) {
      az cosmosdb sql role assignment delete -g $group -a $account --role-assignment-id $id --yes
      if ($LASTEXITCODE -ne 0) { throw "Could not remove task-owned probe grant $id." }
    }
  }
}
