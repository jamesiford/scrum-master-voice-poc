# Focused lifecycle regressions execute the real hook without cloud mutations.
$ErrorActionPreference = 'Stop'
$hook = "$PSScriptRoot\..\infra\hooks\foundry-cleanup.ps1"
$global:foundryCleanupFixture = @{
  calls = [Collections.Generic.List[string]]::new()
  groupExists = 'true'
  projectMissing = $false
  responses = [Collections.Generic.Queue[object]]::new()
}
$calls = $global:foundryCleanupFixture.calls
function az {
  $global:LASTEXITCODE = 0
  switch ("$($args[0]) $($args[1])") {
    'account show' { '{"id":"4b0dd31f-5379-4acc-8dcd-60f2929d2513","tenantId":"tenant"}' }
    'group exists' { $global:foundryCleanupFixture.groupExists }
    'resource show' {
      if ($global:foundryCleanupFixture.projectMissing) {
        $global:LASTEXITCODE = 1
        'ERROR: (ResourceNotFound) The intended project is absent.'
      }
      else { '{"location":"westus3","tags":{"azd-env-name":"zavag9"}}' }
    }
    'account get-access-token' { 'test-token' }
    default { throw "Unexpected CLI invocation: $args" }
  }
}
function Invoke-WebRequest {
  param($Method, $Uri, $Headers, [switch]$SkipHttpErrorCheck, $TimeoutSec)
  $calls.Add("$Method $Uri")
  if ($global:foundryCleanupFixture.responses.Count -eq 0) { throw 'Unexpected lifecycle request.' }
  $response = $global:foundryCleanupFixture.responses.Dequeue()
  if ($response.StatusCode -eq 404 -and -not $response.Content) {
    $name = ([uri]$Uri).Segments[-1]
    $response.Content = @{ error = @{ code = 'not_found'; message = "Agent $name doesn't exist [Request ID: fixture]" } } | ConvertTo-Json -Compress
  }
  $response
}
function Start-Sleep { param($Seconds) }
function Reset-Case {
  $calls.Clear()
  $global:foundryCleanupFixture.groupExists = 'true'
  $global:foundryCleanupFixture.projectMissing = $false
  $global:foundryCleanupFixture.responses = [Collections.Generic.Queue[object]]::new()
  $env:AZURE_ENV_NAME = 'zavag9'
  $env:AZURE_LOCATION = 'westus3'
  $env:AZURE_SUBSCRIPTION_ID = '4b0dd31f-5379-4acc-8dcd-60f2929d2513'
  $env:AZURE_TENANT_ID = 'tenant'
  $env:AZURE_RESOURCE_GROUP = 'rg-zava-scrum-zavag9'
  $env:FOUNDRY_NAME = 'account'
  $env:FOUNDRY_PROJECT_NAME = 'project'
  $env:FOUNDRY_PROJECT_ENDPOINT = 'https://account.services.ai.azure.com/api/projects/project'
  $env:AZURE_CONTAINER_REGISTRY_ENDPOINT = 'registry.azurecr.io'
}
function Add-Response($Status, $Content = '') { $global:foundryCleanupFixture.responses.Enqueue(@{ StatusCode = $Status; Content = $Content }) }
function Assert-Refused([string]$Expected) {
  $message = $null
  try { & $hook } catch { $message = $_.Exception.Message }
  if (-not $message -or $message -notlike "*$Expected*") { throw "Expected refusal '$Expected', got '$message'." }
}
$owned = '{"name":"zava-voice-relay","versions":{"latest":{"definition":{"kind":"hosted","container_configuration":{"image":"registry.azurecr.io/zava-scrum-agent/agenthost:tag"}}}}}'

foreach ($name in @('zavaw3', 'mcm', 'zavagf', 'zavag8')) {
  Reset-Case
  $env:AZURE_ENV_NAME = $name
  Assert-Refused 'authorised only'
  if ($calls.Count) { throw 'Protected environment reached the data plane.' }
}
Reset-Case
$env:FOUNDRY_PROJECT_ENDPOINT = 'https://another.services.ai.azure.com/api/projects/project'
Assert-Refused 'mismatched project endpoint'
Reset-Case
$global:foundryCleanupFixture.groupExists = 'false'
& $hook
if ($calls.Count) { throw 'Missing group reached the data plane.' }
Reset-Case
$global:foundryCleanupFixture.projectMissing = $true
& $hook
if ($calls.Count) { throw 'Missing project reached the data plane.' }
Reset-Case
Add-Response 404
Add-Response 404
& $hook
if ($calls.Count -ne 2) { throw 'Missing agents must not be deleted.' }
Reset-Case
Add-Response 200 $owned
Add-Response 202
Add-Response 200 $owned
Add-Response 404
Add-Response 404
& $hook
if ($calls.Count -ne 5 -or $calls[1] -notlike 'DELETE *') { throw 'Deletion/readback order was not preserved.' }
Reset-Case
Add-Response 200 ($owned.Replace('registry.azurecr.io', 'unrelated.azurecr.io'))
Assert-Refused 'image ownership'
if ($calls.Count -ne 1) { throw 'Unowned image must not be deleted.' }
foreach ($status in @(403, 409)) {
  Reset-Case
  Add-Response 200 $owned
  Add-Response $status 'exact refusal'
  Assert-Refused "HTTP $status`: exact refusal"
  if ($calls.Count -ne 2) { throw 'Refused deletion must not retry.' }
}
Reset-Case
Add-Response 404 '{"error":{"code":"NotFound","message":"Project not found"}}'
Assert-Refused 'cannot prove agent absence'
if ($calls.Count -ne 1) { throw 'Missing data-plane project must not be treated as a missing agent or retried.' }
foreach ($body in @(
  '{"error":{"code":"ResourceNotFound","message":"Subdomain does not map to a resource"}}',
  '{"error":{"code":"UserError","message":"The project does not exist","details":[{"code":"WorkspaceNotFound"}]}}',
  '{"error":{"code":"not_found","message":"Agent unrelated does not exist"}}'
)) {
  Reset-Case
  Add-Response 404 $body
  Assert-Refused 'cannot prove agent absence'
  if ($calls.Count -ne 1) { throw 'A routing, workspace or unrelated-agent 404 must not be absence or trigger a retry.' }
}
Reset-Case
Add-Response 200 $owned
Add-Response 202
Add-Response 404 '{"error":{"code":"NotFound","message":"Project not found"}}'
Assert-Refused 'cannot prove agent absence'
if ($calls.Count -ne 3) { throw 'Lost project visibility must not count as successful deletion readback.' }
Reset-Case
Add-Response 200 $owned
Add-Response 202
for ($i = 0; $i -lt 60; $i++) { Add-Response 200 $owned }
Assert-Refused 'still visible'
if ($calls.Count -ne 62) { throw 'Deletion visibility polling exceeded its bound.' }
Write-Host 'PASS 18 focused Foundry cleanup checks'
Remove-Variable -Name foundryCleanupFixture -Scope Global
