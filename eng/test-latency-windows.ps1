# Adjacent fixed-shape windows; retain failures rather than rerunning a cell to improve its percentile.
param(
  [ValidateSet('before', 'after')][string]$Phase = 'before',
  [ValidateRange(1, 10)][int]$Windows = 3,
  [ValidateRange(1, 5)][int]$SessionsPerWindow = 2,
  [string]$Output = '.m11-plan\run-log\r2-latency-before'
)
$ErrorActionPreference = 'Stop'
foreach ($line in (azd env get-values --environment zavaw3 2>$null)) {
  if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)="?(.*?)"?$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2]) }
}
if ($LASTEXITCODE -ne 0 -or $env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3' -or
    $env:AZURE_RESOURCE_GROUP -ne 'rg-zava-scrum-zavaw3' -or $env:AZURE_SUBSCRIPTION_ID -ne '4b0dd31f-5379-4acc-8dcd-60f2929d2513') {
  throw 'Latency measurement is restricted to the authorised zavaw3/westus3 environment.'
}
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$outputPath = [IO.Path]::GetFullPath($Output, $repo)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$target = @('--subscription', $env:AZURE_SUBSCRIPTION_ID, '-g', $env:AZURE_RESOURCE_GROUP, '-n', $env:API_APP_NAME)
function Read-App {
  $json = az containerapp show @target -o json
  if ($LASTEXITCODE -ne 0) { throw 'Cannot read API configuration; no authentication retry.' }
  return $json | ConvertFrom-Json
}
function Wait-Ready {
  $deadline = [DateTimeOffset]::UtcNow.AddMinutes(5)
  do {
    $app = Read-App
    if ($app.properties.latestRevisionName -eq $app.properties.latestReadyRevisionName -and
        (Invoke-WebRequest "$($env:API_URI.TrimEnd('/'))/healthz" -TimeoutSec 20).StatusCode -eq 200) { return $app }
    Start-Sleep -Seconds 5
  } while ([DateTimeOffset]::UtcNow -lt $deadline)
  throw 'API revision did not become ready within five minutes.'
}
$original = Read-App
$image = $original.properties.template.containers[0].image
$source = [regex]::Match($image, ':git-([0-9a-f]{7,40})@sha256:[0-9a-f]{64}$')
if (-not $source.Success) { throw 'Live API source must be digest-pinned; no local-git substitution.' }
$modeEntry = @($original.properties.template.containers[0].env | Where-Object name -eq 'UtteranceAnalysis__Mode')
if ($modeEntry.Count -gt 1 -or ($modeEntry.Count -eq 1 -and $modeEntry[0].secretRef)) { throw 'Unexpected analysis mode configuration.' }
$original | ConvertTo-Json -Depth 100 | Set-Content (Join-Path $outputPath 'original-api.json')
$results = @()
try {
  for ($window = 1; $window -le $Windows; $window++) {
    $modes = if ($window % 2) { 'Model', 'Rules' } else { 'Rules', 'Model' }
    foreach ($mode in $modes) {
      $current = Read-App
      if ($current.properties.template.containers[0].image -ne $image) { throw 'Live image changed during the controlled cohort.' }
      az containerapp update @target --set-env-vars "UtteranceAnalysis__Mode=$mode" -o none
      if ($LASTEXITCODE -ne 0) { throw "Mode selection failed: $mode; no deployment retry." }
      $ready = Wait-Ready
      if (@($ready.properties.template.containers[0].env | Where-Object { $_.name -eq 'UtteranceAnalysis__Mode' -and $_.value -eq $mode }).Count -ne 1) {
        throw 'Selected mode readback failed.'
      }
      Start-Sleep -Seconds 20
      for ($session = 1; $session -le $SessionsPerWindow; $session++) {
        $env:GATE_TOKEN = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
        if ($LASTEXITCODE -ne 0 -or -not $env:GATE_TOKEN) { throw 'API credential refused; no authentication retry.' }
        $directory = Join-Path $outputPath ("window-{0:d2}-{1}-session-{2:d2}" -f $window, $mode.ToLowerInvariant(), $session)
        $started = [DateTimeOffset]::UtcNow
        dotnet run --project (Join-Path $repo 'tools\GateRunner') -c Release --no-build -- `
          --label "$Phase-direct-$($mode.ToLowerInvariant())" --build $source.Groups[1].Value --runs 1 `
          --api $env:API_URI --team team-payments --out $directory --timeout-minutes 20 `
          *> "$directory.log"
        $exit = $LASTEXITCODE
        $files = @(Get-ChildItem -LiteralPath $directory -Filter "$Phase-direct-*.json" -ErrorAction SilentlyContinue)
        if ($exit -ne 0 -or $files.Count -ne 1) { throw "Window $window/$mode/session $session failed; retained log, no session retry." }
        $run = Get-Content -LiteralPath $files[0].FullName -Raw | ConvertFrom-Json
        $results += [pscustomobject]@{ window = $window; mode = $mode; session = $session; startedAt = $started; revision = $ready.properties.latestReadyRevisionName; report = $files[0].FullName; turns = $run.Turns.Count; questions = @($run.Turns | Where-Object { $_.Reason -in 'Clarify', 'Unblocker' }).Count; talkOvers = $run.TalkOvers }
        [pscustomobject]@{ phase = $Phase; environment = 'zavaw3'; teamId = 'team-payments'; source = $source.Groups[1].Value; image = $image; windows = $Windows; sessionsPerWindow = $SessionsPerWindow; completed = $results.Count; runs = $results } |
          ConvertTo-Json -Depth 20 | Set-Content (Join-Path $outputPath 'windows.json')
        Write-Host "$Phase window=$window mode=$mode session=$session turns=$($run.Turns.Count) questions=$($results[-1].questions) talkOvers=$($run.TalkOvers)"
      }
    }
  }
} finally {
  Remove-Item Env:\GATE_TOKEN -ErrorAction SilentlyContinue
  if ($modeEntry.Count) { az containerapp update @target --set-env-vars "UtteranceAnalysis__Mode=$($modeEntry[0].value)" -o none }
  else { az containerapp update @target --remove-env-vars UtteranceAnalysis__Mode -o none }
  if ($LASTEXITCODE -ne 0) { throw 'Original analysis mode restoration failed; inspect the retained configuration.' }
  $restored = Wait-Ready
  $restored | ConvertTo-Json -Depth 100 | Set-Content (Join-Path $outputPath 'restored-api.json')
  if ($restored.properties.template.containers[0].image -ne $image) { throw 'Original image preservation failed.' }
}
