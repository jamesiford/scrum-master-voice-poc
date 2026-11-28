param(
  [Parameter(Mandatory)][string]$InputDirectory,
  [Parameter(Mandatory)][string]$OutputFile
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $InputDirectory 'windows.json') -Raw | ConvertFrom-Json
function Measure-Samples($Values) {
  $sorted = @($Values | Sort-Object)
  if (-not $sorted.Count) { return [pscustomobject]@{ p50 = $null; p95 = $null; n = 0 } }
  return [pscustomobject]@{
    p50 = $sorted[[int][Math]::Ceiling($sorted.Count * 0.50) - 1]
    p95 = $sorted[[int][Math]::Ceiling($sorted.Count * 0.95) - 1]
    n = $sorted.Count
  }
}
$hops = 'TotalMs', 'SpeechEndToTranscriptMs', 'AnalyserMs', 'DecisionModelMs', 'OrchestrationMs', 'VoiceFirstAudioMs'
$configurations = foreach ($mode in 'Model', 'Rules') {
  $cells = @($manifest.runs | Where-Object { $_.mode -eq $mode })
  $runs = @($cells | ForEach-Object { Get-Content -LiteralPath $_.report -Raw | ConvertFrom-Json })
  $turns = @($runs | ForEach-Object { $_.Turns })
  $questions = @($turns | Where-Object { $_.Reason -in 'Clarify', 'Unblocker' })
  $windows = @(foreach ($group in ($cells | Group-Object window)) {
    $windowTurns = @($group.Group | ForEach-Object { (Get-Content -LiteralPath $_.report -Raw | ConvertFrom-Json).Turns })
    [pscustomobject]@{ window = [int]$group.Name; sessions = $group.Count; total = Measure-Samples @($windowTurns.TotalMs); questions = Measure-Samples @($windowTurns | Where-Object { $_.Reason -in 'Clarify', 'Unblocker' } | ForEach-Object TotalMs) }
  })
  $total = Measure-Samples @($turns.TotalMs)
  $baseline = if ($mode -eq 'Model') { @{ p50 = 2994; p95 = 4081; n = 36 } } else { @{ p50 = 2828; p95 = 3876; n = 36 } }
  $p50Delta = if ($total.n) { 100 * ($total.p50 / $baseline.p50 - 1) } else { $null }
  $p95Delta = if ($total.n) { 100 * ($total.p95 / $baseline.p95 - 1) } else { $null }
  $windowP50s = @($windows | Where-Object { $_.total.n } | ForEach-Object { $_.total.p50 })
  $spread = $windowP50s | Measure-Object -Minimum -Maximum
  [pscustomobject]@{
    mode = $mode
    sessions = $runs.Count
    windows = $windows
    hops = @(foreach ($hop in $hops) { [pscustomobject]@{ name = $hop; all = Measure-Samples @($turns | ForEach-Object { $_.$hop }); questions = Measure-Samples @($questions | ForEach-Object { $_.$hop }) } })
    talkOvers = ($runs | Measure-Object TalkOvers -Sum).Sum
    modelReadTurns = @($turns | Where-Object { $_.Source -eq 'Model' }).Count
    windowP50SpreadPercent = if ($spread.Minimum -gt 0) { 100 * ($spread.Maximum / $spread.Minimum - 1) } else { $null }
    westus3Baseline = $baseline
    p50DeltaPercent = $p50Delta
    p95DeltaPercent = $p95Delta
    toleranceMet = $total.n -gt 0 -and $p50Delta -le 5 -and $p95Delta -le 10
    absoluteOneSecondMet = $total.n -gt 0 -and $total.p50 -le 1000
    sufficientQuestions = $questions.Count -ge 30 -and $windows.Count -ge 3
  }
}
[pscustomobject]@{
  phase = $manifest.phase
  source = $manifest.source
  environment = $manifest.environment
  teamId = $manifest.teamId
  completedSessions = $manifest.completed
  plannedSessions = 2 * $manifest.windows * $manifest.sessionsPerWindow
  percentile = 'nearest-rank'
  baseline = 'docs/gates/baseline-M10-deployed/summary.md (84498ad, three runs/configuration)'
  configurations = @($configurations)
} | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $OutputFile -Encoding utf8
