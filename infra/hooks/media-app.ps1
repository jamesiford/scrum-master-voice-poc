# Catalogue publication is separate from the organiser's per-meeting RSC consent.
param([Parameter(Mandatory)][string]$Fqdn)
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3') {
  throw 'Media catalogue publication is authorised only for zavaw3/westus3.'
}
if (-not $env:MEDIA_APP_ID) { throw 'Run media-host.ps1 -ConsentOnly first.' }
$work = Join-Path ([IO.Path]::GetTempPath()) ('zava-media-app-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
  $manifest = Get-Content "$PSScriptRoot\..\..\appPackage\media-bot\manifest.template.json" -Raw
  $manifest.Replace('{{MEDIA_APP_ID}}', $env:MEDIA_APP_ID).Replace('{{MEDIA_FQDN}}', $Fqdn) |
    Set-Content (Join-Path $work 'manifest.json') -Encoding utf8
  Copy-Item "$PSScriptRoot\..\..\appPackage\color.png", "$PSScriptRoot\..\..\appPackage\outline.png" -Destination $work
  $package = "$PSScriptRoot\..\..\appPackage\dist\zava-media-bot.zip"
  New-Item -ItemType Directory -Path (Split-Path $package) -Force | Out-Null
  Compress-Archive -Path "$work\manifest.json", "$work\color.png", "$work\outline.png" -DestinationPath $package -Force
  $existing = (Invoke-Graph GET "/appCatalogs/teamsApps?`$filter=externalId eq '$($env:MEDIA_APP_ID)'").value
  if (@($existing).Count -gt 1) { throw 'Multiple native media apps have the same manifest id.' }
  if ($existing) {
    Write-Check PASS 'Media Teams catalogue' "Already published: $($existing[0].id)"
    return
  }
  $published = Invoke-RestMethod -Method Post -Uri 'https://graph.microsoft.com/v1.0/appCatalogs/teamsApps' `
    -Headers @{ Authorization = "Bearer $(Get-GraphToken)" } -ContentType 'application/zip' -InFile $package -TimeoutSec 60
  $verified = Invoke-Graph GET "/appCatalogs/teamsApps/$($published.id)?`$expand=appDefinitions"
  if ($verified.distributionMethod -ne 'organization' -or $verified.externalId -ne $env:MEDIA_APP_ID) {
    throw 'Published media app did not match the dedicated application in the organisation catalogue.'
  }
  Set-AzdValue MEDIA_TEAMS_APP_ID $published.id
  Write-Check PASS 'Media Teams catalogue' "Published and read back: $($published.id)"
}
finally {
  Remove-Item -LiteralPath $work -Recurse -Force
}
