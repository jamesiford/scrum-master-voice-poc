# Post-deploy verification. Prints PASS or FAIL per check and exits non-zero if any check fails.
#   pwsh infra/hooks/verify.ps1 [-Runs 1]
# Needs the deployer to be signed in to az and to hold Azure DevOps access (the same sign-in as azd up).
param([int]$Runs = 1)
. "$PSScriptRoot/lib.ps1"
Import-AzdEnvironment
$failed = 0
function Check([string]$Name, [scriptblock]$Body) {
  try { $detail = & $Body; Write-Check PASS $Name "$detail" }
  catch { Write-Check FAIL $Name $_.Exception.Message; $script:failed++ }
}

$api = $env:API_URI.TrimEnd('/')
$bridge = $env:BRIDGE_URI.TrimEnd('/')
$team = 'team-payments'
$token = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
$auth = @{ Authorization = "Bearer $token" }
$org = Get-AdoOrgName

Check 'API health route' { (Invoke-WebRequest "$api/healthz" -TimeoutSec 30).StatusCode }
Check 'Bridge health route' { (Invoke-WebRequest "$bridge/healthz" -TimeoutSec 30).StatusCode }
Check 'API accepts an Entra bearer token' {
  $t = Invoke-RestMethod "$api/api/teams/$team" -Headers $auth -TimeoutSec 30
  "team $($t.id)"
}
Check 'Bot endpoint rejects unauthenticated calls' {
  $s = (Invoke-WebRequest "$api/api/messages" -Method Post -Body '{}' -ContentType 'application/json' -SkipHttpErrorCheck -TimeoutSec 30).StatusCode
  if ($s -ne 401) { throw "expected 401, got $s" }
  '401'
}

$out = Join-Path ([IO.Path]::GetTempPath()) "zava-verify-$([guid]::NewGuid().ToString('N'))"
$sessionId = $null
Check 'Simulated Room standup with the cast (gate harness subset)' {
  $build = (git -C $PSScriptRoot rev-parse --short HEAD 2>$null)
  dotnet run --project (Join-Path $PSScriptRoot '../../tools/GateRunner') -- --label verify --runs $Runs --api $api --team $team --out $out --build "$build" --token $token --timeout-minutes 20 | Out-Host
  if ($LASTEXITCODE -ne 0) { throw 'GateRunner failed' }
  $json = Get-ChildItem $out -Filter *.json | Select-Object -First 1
  $script:sessionId = (Get-Content $json.FullName -Raw | ConvertFrom-Json).sessionId
  if (-not $script:sessionId) { throw 'no session id in the run result' }
  "session $script:sessionId"
}

# The approver is the caller's Entra identity from the validated token; the body names nobody (ADR 0017).
$script:approvedFollowUp = $null
Check 'Follow-up approved in the portal API and written to Azure DevOps' {
  if (-not $sessionId) { throw 'no session from the previous check' }
  $marker = "verify-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
  $body = @{ sessionId = $sessionId; memberId = 'daniel'; workItemId = 1; op = 'Comment'; value = $marker; rationale = 'post-deploy verification' } | ConvertTo-Json
  $id = (Invoke-RestMethod -Method Post "$api/api/followups/propose" -Headers $auth -ContentType 'application/json' -Body $body -TimeoutSec 60).ToString().Trim('"')
  Invoke-RestMethod -Method Post "$api/api/followups/$id/approve" -Headers $auth -ContentType 'application/json' -Body '{}' -TimeoutSec 60 | Out-Null
  $h = Get-AdoHeaders
  $project = (Get-Content (Join-Path $PSScriptRoot '../../demo/seed-ado.json') -Raw | ConvertFrom-Json).project
  for ($i = 0; $i -lt 6; $i++) {
    $c = Invoke-RestMethod "https://dev.azure.com/$org/$project/_apis/wit/workItems/1/comments?api-version=7.1-preview.4" -Headers $h
    if ($c.comments | Where-Object { ($_.text -like "*$marker*" -and $_.text -like '*Correlation: zsa-*') }) { $script:approvedFollowUp = $id; return "comment $marker on #1 after approval by $($env:ADMIN_UPN)" }
    if ($i -eq 2) { Invoke-RestMethod -Method Post "$api/api/followups/$id/apply" -Headers $auth -TimeoutSec 60 | Out-Null }
    Start-Sleep -Seconds 3
  }
  throw "comment $marker not found on work item 1"
}

Check 'Audit chain complete (session, proposal, approval record, Azure DevOps write)' {
  if (-not $script:approvedFollowUp) { throw 'no approved follow-up from the previous check' }
  $session = Invoke-RestMethod "$api/api/sessions/$sessionId" -Headers $auth -TimeoutSec 60
  if (-not $session.correlationId) { throw 'the session has no correlation id' }
  $chain = Invoke-RestMethod "$api/api/audit/$($session.correlationId)" -Headers $auth -TimeoutSec 60
  if ($chain.missing.Count -gt 0) { throw "chain incomplete: $($chain.missing -join '; ')" }
  $approved = $chain.records | Where-Object { $_.followUpId -eq $script:approvedFollowUp -and $_.kind -eq 'Approved' } | Select-Object -First 1
  $applied = $chain.records | Where-Object { $_.followUpId -eq $script:approvedFollowUp -and $_.kind -eq 'Applied' } | Select-Object -First 1
  if (-not $approved) { throw 'no approval record' }
  $oid = (Get-TokenClaims $token).oid
  if ($approved.actor.objectId -ne $oid) { throw "approval record names $($approved.actor.objectId), not the signed-in object id $oid" }
  if (-not $applied -or $applied.adoRevisions.Count -eq 0) { throw 'no write record with a work item revision' }
  "$($chain.tag): approved by object id $oid, revision $($applied.adoRevisions[0].revision) on #$($applied.adoRevisions[0].workItemId)"
}
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
if ($failed) { Write-Host "`n$failed check(s) failed." -ForegroundColor Red; exit 1 }
Write-Host "`nAll checks passed." -ForegroundColor Green
