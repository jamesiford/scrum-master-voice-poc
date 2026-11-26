<#
  Analyses the interleaved G1 runs of prompt 4A (ADR 0021): per cell totals, paired differences per round (hosted minus direct, same time window),
  a bootstrap 95 % interval for the pooled difference of p50 and p95, and what the slowest turns are made of (orchestration or voice).
    pwsh docs/gates/m11-p4a/analyse.ps1 [-Root docs/gates/m11-p4a/g1]
#>
param([string]$Root = 'docs/gates/m11-p4a/g1', [int]$Resamples = 3000)
$rng = [Random]::new(20261125)
function Pct([double[]]$v, [double]$q) { if ($v.Count -eq 0) { return 0 }; $s = $v | Sort-Object; $s[[Math]::Min($s.Count - 1, [Math]::Ceiling($q * $s.Count) - 1)] }
function Boot([double[]]$a, [double[]]$b, [double]$q) {
  $d = for ($i = 0; $i -lt $Resamples; $i++) {
    $ra = 1..$a.Count | ForEach-Object { $a[$rng.Next($a.Count)] }
    $rb = 1..$b.Count | ForEach-Object { $b[$rng.Next($b.Count)] }
    (Pct $rb $q) - (Pct $ra $q)
  }
  $s = $d | Sort-Object
  [pscustomobject]@{ Lo = $s[[int]($Resamples * 0.025)]; Hi = $s[[int]($Resamples * 0.975) - 1] }
}
$runs = foreach ($f in Get-ChildItem $Root -Recurse -Filter *.json) {
  $j = Get-Content $f.FullName -Raw | ConvertFrom-Json
  if ($j.Label -notmatch '^round(\d+)-(direct|hosted)-(model|rules)$') { continue }
  [pscustomobject]@{ Round = [int]$Matches[1]; Route = $Matches[2]; Mode = $Matches[3]; Turns = @($j.Turns) }
}
# Several teams write several files per run; merge by label.
$runs = $runs | Group-Object Round, Route, Mode | ForEach-Object { [pscustomobject]@{ Round = $_.Group[0].Round; Route = $_.Group[0].Route; Mode = $_.Group[0].Mode; Turns = @($_.Group | ForEach-Object { $_.Turns }) } }
foreach ($mode in 'model', 'rules') {
  "## decision model: $mode"
  $cell = @{}
  foreach ($route in 'direct', 'hosted') {
    $t = @($runs | Where-Object { $_.Mode -eq $mode -and $_.Route -eq $route } | ForEach-Object { $_.Turns })
    $cell[$route] = $t
    $tot = [double[]]($t | ForEach-Object { $_.TotalMs })
    "{0,-7} rounds {1} turns {2}: total p50 {3} p95 {4}; voice first audio p50 {5} p95 {6}; orchestration p50 {7} p95 {8}" -f $route, @($runs | Where-Object { $_.Mode -eq $mode -and $_.Route -eq $route }).Count, $t.Count, (Pct $tot 0.5), (Pct $tot 0.95), (Pct ([double[]]($t | % { $_.VoiceFirstAudioMs })) 0.5), (Pct ([double[]]($t | % { $_.VoiceFirstAudioMs })) 0.95), (Pct ([double[]]($t | % { $_.OrchestrationMs })) 0.5), (Pct ([double[]]($t | % { $_.OrchestrationMs })) 0.95)
  }
  $d = [double[]]($cell.direct | % { $_.TotalMs }); $h = [double[]]($cell.hosted | % { $_.TotalMs })
  foreach ($q in 0.5, 0.95) {
    $diff = (Pct $h $q) - (Pct $d $q); $base = Pct $d $q; $ci = Boot $d $h $q
    "pooled p{0}: hosted minus direct {1:N0} ms ({2:P1} of direct {3:N0}); bootstrap 95 % interval {4:N0} to {5:N0} ms ({6:P1} to {7:P1})" -f ($q * 100), $diff, ($diff / $base), $base, $ci.Lo, $ci.Hi, ($ci.Lo / $base), ($ci.Hi / $base)
  }
  $per = foreach ($r in ($runs | Where-Object Mode -eq $mode | Select-Object -ExpandProperty Round -Unique | Sort-Object)) {
    $dr = [double[]]($runs | Where-Object { $_.Round -eq $r -and $_.Mode -eq $mode -and $_.Route -eq 'direct' } | % { $_.Turns } | % { $_.TotalMs })
    $hr = [double[]]($runs | Where-Object { $_.Round -eq $r -and $_.Mode -eq $mode -and $_.Route -eq 'hosted' } | % { $_.Turns } | % { $_.TotalMs })
    if ($dr.Count -and $hr.Count) { [pscustomobject]@{ Round = $r; P50 = (Pct $hr 0.5) - (Pct $dr 0.5); P95 = (Pct $hr 0.95) - (Pct $dr 0.95) } }
  }
  foreach ($q in 'P50', 'P95') {
    $v = [double[]]($per | % { $_.$q }); $m = ($v | Measure-Object -Average).Average
    $sd = [Math]::Sqrt((($v | % { ($_ - $m) * ($_ - $m) } | Measure-Object -Sum).Sum) / [Math]::Max(1, $v.Count - 1)); $se = $sd / [Math]::Sqrt($v.Count)
    "per-round {0}: {1} | mean {2:N0} sd {3:N0} se {4:N0}; rounds hosted slower {5} of {6}" -f $q, (($per | % { '{0:N0}' -f $_.$q }) -join ', '), $m, $sd, $se, @($v | ? { $_ -gt 0 }).Count, $v.Count
  }
  foreach ($route in 'direct', 'hosted') {
    $t = @($cell[$route] | Sort-Object TotalMs -Descending); $n = [Math]::Max(3, [int]($t.Count * 0.05)); $top = $t | Select-Object -First $n
    $orchShare = ($top | % { $_.OrchestrationMs / $_.TotalMs } | Measure-Object -Average).Average; $voiceShare = ($top | % { $_.VoiceFirstAudioMs / $_.TotalMs } | Measure-Object -Average).Average
    "slowest 5 % ($n turns) of {0}: mean total {1:N0}; orchestration {2:P0} of the turn, voice first audio {3:P0}; orchestration median of these {4:N0}, voice median {5:N0}" -f $route, ($top | Measure-Object TotalMs -Average).Average, $orchShare, $voiceShare, (Pct ([double[]]($top | % { $_.OrchestrationMs })) 0.5), (Pct ([double[]]($top | % { $_.VoiceFirstAudioMs })) 0.5)
  }
}
