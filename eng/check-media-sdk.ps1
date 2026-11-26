param(
  [string]$FeedIndex = 'https://api.nuget.org/v3/index.json',
  [datetimeoffset]$Now = [datetimeoffset]::UtcNow
)
$ErrorActionPreference = 'Stop'
$package = 'microsoft.graph.communications.calls.media'
[xml]$versions = Get-Content "$PSScriptRoot\..\Directory.Packages.props"
$pinned = ($versions.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq 'Microsoft.Graph.Communications.Calls.Media' }).Version
if (-not $pinned) { throw 'The media SDK must have a centrally pinned version.' }
$index = Invoke-RestMethod $FeedIndex -TimeoutSec 30
$base = ($index.resources | Where-Object { $_.'@type' -like 'PackageBaseAddress*' } | Select-Object -First 1).'@id'
$registration = ($index.resources | Where-Object { $_.'@type' -like 'RegistrationsBaseUrl/3.6*' } | Select-Object -First 1).'@id'
if (-not $registration) { $registration = ($index.resources | Where-Object { $_.'@type' -like 'RegistrationsBaseUrl*' } | Select-Object -First 1).'@id' }
if (-not $base -or -not $registration) { throw 'The package feed exposes no version or publication metadata.' }
$available = (Invoke-RestMethod "$base$package/index.json" -TimeoutSec 30).versions
$latest = $available | Where-Object { $_ -match '^\d+(\.\d+){2,3}$' } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1
if (-not $latest -or $available -notcontains $pinned) { throw 'Cannot verify the media SDK version against this feed.' }
$leaf = Invoke-RestMethod "$registration$package/$pinned.json" -TimeoutSec 30
$entry = $leaf.catalogEntry
$published = if ($leaf.published) { [datetimeoffset]$leaf.published } else {
  if ($entry -is [string]) { $entry = Invoke-RestMethod $entry -TimeoutSec 30 }
  if (-not $entry.published) { throw 'Package publication timestamp is absent.' }
  [datetimeoffset]$entry.published
}
if ($pinned -ne $latest -and $published -lt $Now.AddMonths(-3)) {
  throw "Media SDK $pinned was published $published and is more than three months old; latest is $latest. Update Directory.Packages.props."
}
Write-Output "PASS: media SDK $pinned, published $($published.ToString('o')), latest $latest. Newest or within three months."
