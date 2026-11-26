# Native hosting checks are separate from Teams acceptance: a missing trusted certificate is an operator gate.
param([switch]$RequireReady)
. "$PSScriptRoot\lib.ps1"
Import-AzdEnvironment
if ($env:MEDIA_HOST_ENABLED -ne 'true') {
  Write-Check SKIP 'Windows media hosting' 'MEDIA_HOST_ENABLED is false'
  return
}
if ($env:AZURE_ENV_NAME -ne 'zavaw3' -or $env:AZURE_LOCATION -ne 'westus3') {
  throw 'Native media verification is authorised only for zavaw3/westus3.'
}
foreach ($name in @('MEDIA_VM_NAME', 'MEDIA_FQDN', 'MEDIA_PUBLIC_IP', 'MEDIA_VAULT_NAME')) {
  if (-not [Environment]::GetEnvironmentVariable($name)) { throw "$name is missing; provision the media module first." }
}
$vm = az vm get-instance-view -g $env:AZURE_RESOURCE_GROUP -n $env:MEDIA_VM_NAME -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Cannot read the Windows media VM instance view.' }
if (-not ($vm.instanceView.statuses | Where-Object code -eq 'PowerState/running')) {
  throw 'The Windows media VM is not running.'
}
Write-Check PASS 'Windows media VM' "$($vm.hardwareProfile.vmSize), running"
$addresses = [Net.Dns]::GetHostAddresses($env:MEDIA_FQDN).IPAddressToString
if ($env:MEDIA_PUBLIC_IP -notin $addresses) { throw 'The media FQDN does not resolve to the provisioned instance public IP.' }
Write-Check PASS 'Media public DNS' "$($env:MEDIA_FQDN) resolves to $($env:MEDIA_PUBLIC_IP)"
$certificate = az keyvault certificate show --vault-name $env:MEDIA_VAULT_NAME --name media-tls --query '{enabled:attributes.enabled,expires:attributes.expires}' -o json 2>&1
if ($LASTEXITCODE -ne 0) {
  if (($certificate -join "`n") -notmatch 'CertificateNotFound') { throw ($certificate -join "`n") }
  $gate = "Import a CA-trusted PFX for $($env:MEDIA_FQDN) as media-tls in $($env:MEDIA_VAULT_NAME), then rerun media-host.ps1. See docs/DEPLOY.md, MEDIA-TLS."
  Write-Check GATE 'MEDIA-TLS: native readiness NOT verified' $gate
  if ($RequireReady) { throw $gate }
  return
}
$attributes = $certificate | ConvertFrom-Json
if (-not $attributes.enabled) { throw 'The media TLS certificate is disabled.' }
$health = Invoke-RestMethod "https://$($env:MEDIA_FQDN)/healthz" -TimeoutSec 30
if ($health.service -ne 'media-bot' -or $health.recording -ne $false -or $health.maxConcurrentMeetings -ne 1) {
  throw 'Native HTTPS readiness must report media-bot, no recording and the one-meeting cap.'
}
Write-Check PASS 'Native HTTPS readiness' "https://$($env:MEDIA_FQDN)/healthz; no recording; cap 1"
$uri = "$($env:API_URI)/api/media/context?sessionId=$([Guid]::NewGuid())&teamId=verification"
$status = (Invoke-WebRequest $uri -SkipHttpErrorCheck -MaximumRedirection 0 -TimeoutSec 30).StatusCode
if ($status -ne 401) { throw "Native API route must reject unauthenticated requests with 401, not $status or an Easy Auth redirect." }
Write-Check PASS 'Native API capability protection' 'Unauthenticated media context receives 401 through the independent ASP.NET scheme.'
Write-Check GATE 'Native Teams acceptance' 'HTTPS readiness does not prove RSC, G6 joins, G2 attribution, barge-in or native G1. See docs/gates/M11-gates.md.'
