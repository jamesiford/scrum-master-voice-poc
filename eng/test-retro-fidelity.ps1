# Real Simulated Room retrospectives, not a prompt-only model probe. Refresh API credentials between complete runs.
param(
  [ValidateRange(1, 100)][int]$Runs = 30,
  [string]$Output = '.m11-plan\run-log\r2-quality-retros'
)
$ErrorActionPreference = 'Stop'
foreach ($line in (azd env get-values --environment zavaw3 2>$null)) {
  if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)="?(.*?)"?$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2]) }
}
if ($LASTEXITCODE -ne 0 -or $env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3') {
  throw 'Retrospective fidelity measurement is restricted to zavaw3/westus3.'
}
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$outputPath = [IO.Path]::GetFullPath($Output, $repo)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$results = @()
try {
  for ($run = 1; $run -le $Runs; $run++) {
    $env:GATE_TOKEN = az account get-access-token --scope "api://$($env:PORTAL_CLIENT_ID)/access_as_user" --query accessToken -o tsv
    if ($LASTEXITCODE -ne 0 -or -not $env:GATE_TOKEN) { throw 'API credential refused; no authentication retry or synthetic substitution.' }
    $directory = Join-Path $outputPath ("run-{0:d2}" -f $run)
    $started = [DateTimeOffset]::UtcNow
    dotnet run --project (Join-Path $repo 'tools\GateRunner') -c Release --no-build -- `
      --retro --label r2-quality-retro --runs 1 --api $env:API_URI --team team-payments --out $directory --timeout-minutes 20 `
      *> (Join-Path $outputPath ("run-{0:d2}.log" -f $run))
    $exit = $LASTEXITCODE
    $files = @(Get-ChildItem -LiteralPath $directory -Filter 'r2-quality-retro-*.json' -ErrorAction SilentlyContinue)
    $proof = if ($files.Count -eq 1) { (Get-Content -LiteralPath $files[0].FullName -Raw | ConvertFrom-Json).Retrospective } else { $null }
    $results += [pscustomobject]@{ run = $run; startedAt = $started; seconds = ([DateTimeOffset]::UtcNow - $started).TotalSeconds; exitCode = $exit; proof = $proof }
    [pscustomobject]@{ environment = 'zavaw3'; requestedRuns = $Runs; completedRuns = $results.Count; passedRuns = @($results | Where-Object { $_.exitCode -eq 0 -and $_.proof.Passed }).Count; runs = $results } |
      ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $outputPath 'fidelity.json') -Encoding utf8
    Write-Host "Retro $run/$Runs exit=$exit circle-back=$($proof.Spoken)/$($proof.CircledBack) answered=$($proof.Answered); no retry."
    if ($exit -ne 0 -and -not $proof) { throw "Run $run did not produce a complete real-session report; inspect its retained log." }
  }
} finally { Remove-Item Env:\GATE_TOKEN -ErrorAction SilentlyContinue }
if (@($results | Where-Object { $_.exitCode -ne 0 -or -not $_.proof.Passed }).Count) { exit 1 }
