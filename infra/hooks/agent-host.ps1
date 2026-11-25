# Runs in `postdeploy`: builds the agent host image in the environment's registry, creates a new immutable version of the Foundry hosted
# agent, gives the agent's own identity the roles it needs (least privilege) and warms it up (ADR 0021). Idempotent: a re-run adds a version
# and leaves the role assignments as they are. The traffic split is not changed here; `canary.ps1` does that.
#   pwsh infra/hooks/agent-host.ps1 [-Tag <image tag>] [-Canary]
# -Canary builds the same source into a second hosted agent (<name>-canary) that canary.ps1 can send a share of sessions to.
param([string]$Tag = (Get-Date -Format 'yyyyMMddHHmmss'), [switch]$Canary)
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment

$repo = Resolve-Path (Join-Path $PSScriptRoot '../..')
$registry = ($env:AZURE_CONTAINER_REGISTRY_ENDPOINT -split '\.')[0]
$account = $env:FOUNDRY_NAME
$project = $env:FOUNDRY_PROJECT_NAME
$endpoint = $env:FOUNDRY_PROJECT_ENDPOINT
$agent = if ($Canary) { 'zava-voice-relay-canary' } else { 'zava-voice-relay' }
$image = "$($env:AZURE_CONTAINER_REGISTRY_ENDPOINT)/zava-scrum-agent/agenthost:$Tag"

# The build context holds only what the image needs, so the upload is a few kilobytes instead of the whole repository.
$ctx = Join-Path ([IO.Path]::GetTempPath()) "zava-agenthost-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $ctx | Out-Null
try {
  foreach ($f in 'global.json', 'Directory.Build.props', 'Directory.Packages.props', '.editorconfig', 'Dockerfile.agenthost') { Copy-Item (Join-Path $repo $f) $ctx }
  New-Item -ItemType Directory (Join-Path $ctx 'src') | Out-Null
  Copy-Item (Join-Path $repo 'src/Zava.Scrum.AgentHost') (Join-Path $ctx 'src') -Recurse
  Get-ChildItem (Join-Path $ctx 'src/Zava.Scrum.AgentHost') -Directory | Where-Object { $_.Name -in 'bin', 'obj' } | Remove-Item -Recurse -Force
  az acr build --registry $registry --image "zava-scrum-agent/agenthost:$Tag" --file Dockerfile.agenthost $ctx -o none
  if ($LASTEXITCODE -ne 0) { throw 'The agent host image build failed.' }
}
finally { Remove-Item $ctx -Recurse -Force -ErrorAction SilentlyContinue }
Write-Check PASS 'agent host image' $image

# The Voice Live endpoint and model are plain settings of the version; there is no secret. The agent signs in with its own identity.
$model = if ($env:VOICELIVE_MODEL) { $env:VOICELIVE_MODEL } else { 'gpt-realtime-2.1' }
$out = dotnet run --project (Join-Path $repo 'tools/AgentDeploy') -- --endpoint $endpoint --name $agent --image $image --tenant $env:AZURE_TENANT_ID `
  --env "VOICELIVE_ENDPOINT=wss://$account.services.ai.azure.com/voice-live/realtime" --env "VOICELIVE_MODEL=$model" --env 'VOICELIVE_API_VERSION=2026-04-10'
if ($LASTEXITCODE -ne 0) { throw 'Creating the hosted agent version failed.' }
$version = ($out | Where-Object { $_ -like 'VERSION=*' } | Select-Object -Last 1).Split('=')[1]
if ($Canary) { Set-AzdValue AGENT_HOST_CANARY_VERSION $version }
else {
  Set-AzdValue AGENT_HOST_NAME $agent
  Set-AzdValue AGENT_HOST_VERSION $version
}
Write-Check PASS 'hosted agent version' "$agent version $version (immutable)"

# The agent instance identity is created with the agent, so its roles cannot be in the Bicep that runs before it exists.
$token = az account get-access-token --resource https://ai.azure.com --query accessToken -o tsv
$info = Invoke-RestMethod "$endpoint/agents/$($agent)?api-version=v1" -Headers @{ Authorization = "Bearer $token" }
$principal = $info.instance_identity.principal_id
$scope = "/subscriptions/$($env:AZURE_SUBSCRIPTION_ID)/resourceGroups/$($env:AZURE_RESOURCE_GROUP)/providers/Microsoft.CognitiveServices/accounts/$account"
foreach ($role in @(@{ Name = 'Cognitive Services User'; Id = 'a97b65f3-24c7-4388-baec-2e87135dc908' }, @{ Name = 'Azure AI User'; Id = '53ca6127-db72-4b80-b1b0-d745d6d5456d' })) {
  $has = az role assignment list --assignee $principal --scope $scope --role $role.Id --query '[0].id' -o tsv
  if (-not $has) {
    az role assignment create --assignee-object-id $principal --assignee-principal-type ServicePrincipal --role $role.Id --scope $scope -o none
    Write-Check PASS "agent identity role $($role.Name)" 'assigned on the Foundry account'
  }
  else { Write-Check PASS "agent identity role $($role.Name)" 'already assigned' }
}
if (-not $Canary) { Set-AzdValue AGENT_HOST_PRINCIPAL_ID $principal }
Write-Check PASS 'agent host' "wss://$account.services.ai.azure.com/api/projects/$project/agents/$agent/endpoint/protocols/invocations_ws"
