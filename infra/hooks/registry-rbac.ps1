# The empty-environment up path must confirm registry permissions before azd deploy pulls its first private image.
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
foreach ($name in @('AZURE_SUBSCRIPTION_ID', 'AZURE_RESOURCE_GROUP', 'AZURE_CONTAINER_REGISTRY_ENDPOINT', 'MANAGED_IDENTITY_PRINCIPAL_ID')) {
  if (-not [Environment]::GetEnvironmentVariable($name)) { throw "$name is missing; provision the registry and identity first." }
}
$registryName = ($env:AZURE_CONTAINER_REGISTRY_ENDPOINT -split '\.')[0]
$registry = az acr show --subscription $env:AZURE_SUBSCRIPTION_ID -g $env:AZURE_RESOURCE_GROUP -n $registryName --query id -o tsv
if ($LASTEXITCODE -ne 0 -or -not $registry) { throw 'Cannot resolve the provisioned registry.' }
for ($attempt = 1; $attempt -le 5; $attempt++) {
  $roles = az role assignment list --subscription $env:AZURE_SUBSCRIPTION_ID --scope $registry `
    --assignee-object-id $env:MANAGED_IDENTITY_PRINCIPAL_ID --query "[?roleDefinitionName=='AcrPull'].roleDefinitionName" -o tsv
  if ($LASTEXITCODE -ne 0) { throw 'Cannot read the registry-scoped image pull role.' }
  if ($roles -contains 'AcrPull') { Write-Check PASS 'registry image pull role' 'AcrPull visible at the provisioned registry scope'; return }
  if ($attempt -lt 5) { Start-Sleep -Seconds 60 }
}
throw 'Registry-scoped AcrPull is not visible after five checks; image deployment is refused.'
