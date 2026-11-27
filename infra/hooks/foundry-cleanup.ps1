# Delete owned hosted data-plane resources before their project identity is destroyed (ADR 0035).
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

if ($env:AZURE_ENV_NAME -ne 'zavagf' -or $env:AZURE_LOCATION -ne 'eastus2' -or
  $env:AZURE_SUBSCRIPTION_ID -ne '4b0dd31f-5379-4acc-8dcd-60f2929d2513' -or
  $env:AZURE_RESOURCE_GROUP -ne 'rg-zava-scrum-zavagf') {
  throw 'Foundry teardown is authorised only for zavagf/eastus2 in its exact resource group and subscription.'
}
$account = az account show -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $account.id -ne $env:AZURE_SUBSCRIPTION_ID -or $account.tenantId -ne $env:AZURE_TENANT_ID) {
  throw 'Foundry teardown account does not match the authorised environment.'
}
$exists = az group exists --subscription $env:AZURE_SUBSCRIPTION_ID -n $env:AZURE_RESOURCE_GROUP -o tsv
if ($LASTEXITCODE -ne 0) { throw 'Cannot check the intended resource group before Foundry cleanup.' }
if ($exists -eq 'false') {
  Write-Check SKIP 'Foundry cleanup' 'The intended resource group is already absent.'
  return
}
if (-not $env:FOUNDRY_NAME -or -not $env:FOUNDRY_PROJECT_NAME -or -not $env:AZURE_CONTAINER_REGISTRY_ENDPOINT) {
  throw 'Foundry cleanup requires the existing account, project and registry outputs.'
}
$projectId = "/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$($env:AZURE_RESOURCE_GROUP)/providers/Microsoft.CognitiveServices/accounts/$($env:FOUNDRY_NAME)/projects/$($env:FOUNDRY_PROJECT_NAME)"
$expectedEndpoint = "https://$($env:FOUNDRY_NAME).services.ai.azure.com/api/projects/$($env:FOUNDRY_PROJECT_NAME)"
if ($env:FOUNDRY_PROJECT_ENDPOINT -ne $expectedEndpoint) { throw 'Foundry cleanup refuses a mismatched project endpoint.' }
$projectOutput = az resource show --ids $projectId --api-version 2025-06-01 -o json 2>&1
if ($LASTEXITCODE -ne 0) {
  if (($projectOutput -join "`n") -match '\(ResourceNotFound\)') {
    Write-Check SKIP 'Foundry cleanup' 'The intended project is already absent.'
    return
  }
  throw "Cannot read the intended Foundry project: $($projectOutput -join "`n")"
}
$project = $projectOutput | ConvertFrom-Json
if ($project.tags.'azd-env-name' -ne 'zavagf' -or $project.location -ne 'eastus2') {
  throw 'Cannot verify the intended Foundry project ownership before cleanup.'
}
$token = az account get-access-token --resource https://ai.azure.com --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or -not $token) { throw 'Cannot authenticate the Foundry cleanup; no authentication retry.' }

function Invoke-FoundryLifecycleRequest([string]$Method, [string]$Uri) {
  $response = Invoke-WebRequest -Method $Method -Uri $Uri -Headers @{ Authorization = "Bearer $token" } `
    -SkipHttpErrorCheck -TimeoutSec 60
  if ($response.StatusCode -notin @(200, 202, 204, 404)) {
    throw "Foundry cleanup $Method failed HTTP $($response.StatusCode): $($response.Content)"
  }
  if ($response.StatusCode -eq 404 -and $response.Content -match '"message"\s*:\s*"Project not found"') {
    throw "Foundry cleanup cannot prove agent absence: the ARM project exists but its data plane returned HTTP 404: $($response.Content)"
  }
  return $response
}

foreach ($name in @('zava-voice-relay', 'zava-voice-relay-canary')) {
  $uri = "$expectedEndpoint/agents/$($name)?api-version=v1"
  $response = Invoke-FoundryLifecycleRequest GET $uri
  if ($response.StatusCode -eq 404) {
    Write-Check SKIP 'Foundry agent cleanup' "$name is already absent."
    continue
  }
  $agent = $response.Content | ConvertFrom-Json
  $imagePrefix = "$($env:AZURE_CONTAINER_REGISTRY_ENDPOINT)/zava-scrum-agent/agenthost:"
  $image = [string]$agent.versions.latest.definition.container_configuration.image
  if ($agent.name -ne $name -or $agent.versions.latest.definition.kind -ne 'hosted' -or
    -not $image.StartsWith($imagePrefix, [StringComparison]::Ordinal)) {
    throw "Foundry cleanup refuses agent $name without the intended hosted image ownership."
  }
  # https://learn.microsoft.com/azure/foundry/agents/how-to/manage-hosted-agent#delete-an-agent
  Invoke-FoundryLifecycleRequest DELETE $uri | Out-Null
  $absent = $false
  for ($poll = 0; $poll -lt 60; $poll++) {
    if ((Invoke-FoundryLifecycleRequest GET $uri).StatusCode -eq 404) { $absent = $true; break }
    Start-Sleep -Seconds 5
  }
  if (-not $absent) { throw "Foundry agent $name is still visible after five minutes; refusing project teardown." }
  Write-Check PASS 'Foundry agent cleanup' "$name deleted and absence read back before project teardown."
}
