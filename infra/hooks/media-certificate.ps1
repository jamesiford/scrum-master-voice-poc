param(
  [Parameter(Mandatory)][string]$Fqdn,
  [Parameter(Mandatory)][string]$VaultName
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$root = 'C:\ProgramData\ZavaMedia'
New-Item -ItemType Directory -Path $root -Force | Out-Null
& icacls.exe $root /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'BUILTIN\Administrators:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Cannot protect the ACME certificate directory.' }
$env:POSHACME_HOME = Join-Path $root 'acme'
New-Item -ItemType Directory -Path $env:POSHACME_HOME -Force | Out-Null
if (-not (Get-Module -ListAvailable Posh-ACME)) {
  Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force | Out-Null
  Install-Module Posh-ACME -RequiredVersion 4.32.0 -Scope AllUsers -Repository PSGallery -Force
}
Import-Module Posh-ACME -RequiredVersion 4.32.0
if (-not (Get-NetFirewallRule -Name 'ZavaMediaAcme' -ErrorAction SilentlyContinue)) {
  New-NetFirewallRule -Name 'ZavaMediaAcme' -DisplayName 'Zava media HTTP-01 challenge' -Direction Inbound `
    -Action Allow -Protocol TCP -LocalPort 80 | Out-Null
}
# SYSTEM is the only listener identity; the plugin closes the HTTP listener after validation.
$reservation = 'http://+:80/.well-known/acme-challenge/'
if (((& netsh.exe http show urlacl) -join "`n") -notmatch [Regex]::Escape($reservation)) {
  & netsh.exe http add urlacl "url=$reservation" user=SYSTEM | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Cannot reserve the HTTP-01 challenge path.' }
}
$passwordBytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($passwordBytes) } finally { $random.Dispose() }
$pfxPassword = [Convert]::ToBase64String($passwordBytes)
$cert = New-PACertificate $Fqdn -AcceptTOS -DirectoryUrl LE_PROD -Plugin WebSelfHost `
  -PluginArgs @{ WSHTimeout = 180 } -CertKeyLength 2048 -PfxPass $pfxPassword -Force
$uri = 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net'
$token = (Invoke-RestMethod $uri -Headers @{ Metadata = 'true' } -TimeoutSec 30).access_token
$body = @{
  value = [Convert]::ToBase64String([IO.File]::ReadAllBytes($cert.PfxFullChain))
  pwd = $pfxPassword
  policy = @{ key_props = @{ exportable = $true; kty = 'RSA'; key_size = 2048 }; secret_props = @{ contentType = 'application/x-pkcs12' } }
} | ConvertTo-Json -Depth 6
$imported = Invoke-RestMethod -Method Post -Uri "https://$VaultName.vault.azure.net/certificates/media-tls/import?api-version=2025-07-01" `
  -Headers @{ Authorization = "Bearer $token" } -ContentType 'application/json' -Body $body -TimeoutSec 60
Write-Output "MEDIA_TLS_IMPORTED:$($imported.id)"
