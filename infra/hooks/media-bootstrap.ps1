param(
  [Parameter(Mandatory)][string]$StorageName,
  [Parameter(Mandatory)][string]$PackageName,
  [Parameter(Mandatory)][string]$PackageSha256,
  [Parameter(Mandatory)][string]$VaultName,
  [Parameter(Mandatory)][string]$AppId,
  [Parameter(Mandatory)][string]$TenantId,
  [Parameter(Mandatory)][string]$Fqdn,
  [Parameter(Mandatory)][string]$PublicIp,
  [Parameter(Mandatory)][string]$ApiBase
)
$ErrorActionPreference = 'Stop'
$root = 'C:\ProgramData\ZavaMedia'
New-Item -ItemType Directory -Path $root -Force | Out-Null
& icacls.exe $root /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'BUILTIN\Administrators:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Cannot protect the media host directory.' }

function Get-ManagedToken([string]$Resource) {
  $uri = 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=' + [Uri]::EscapeDataString($Resource)
  (Invoke-RestMethod -Uri $uri -Headers @{ Metadata = 'true' } -TimeoutSec 30).access_token
}
function Get-MediaSecret([string]$Name) {
  $token = Get-ManagedToken 'https://vault.azure.net'
  (Invoke-RestMethod -Uri "https://$VaultName.vault.azure.net/secrets/$Name`?api-version=7.4" -Headers @{ Authorization = "Bearer $token" } -TimeoutSec 30).value
}

$certificateBytes = [Convert]::FromBase64String((Get-MediaSecret 'media-tls'))
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList @(
  $certificateBytes, '', ([System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::MachineKeySet -bor
    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet)
)
$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
if (-not $certificate.HasPrivateKey -or -not $chain.Build($certificate) -or $certificate.NotAfter -le (Get-Date).AddDays(14)) {
  throw 'media-tls must be an exportable, trusted, unexpired certificate with its private key and full chain.'
}
if ($certificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::DnsName, $false) -ne $Fqdn) {
  throw "media-tls must name the configured media FQDN ($Fqdn) as its primary DNS name."
}
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store 'My', 'LocalMachine'
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
try { $store.Add($certificate) } finally { $store.Close() }

$task = Get-ScheduledTask -TaskName 'ZavaMediaBot' -ErrorAction SilentlyContinue
if ($task) {
  Stop-ScheduledTask -TaskName 'ZavaMediaBot'
  Start-Sleep -Seconds 3
}
$package = Join-Path $root 'media-package.zip'
$token = Get-ManagedToken 'https://storage.azure.com/'
Invoke-WebRequest -UseBasicParsing -Uri "https://$StorageName.blob.core.windows.net/media-packages/$PackageName" `
  -Headers @{ Authorization = "Bearer $token"; 'x-ms-version' = '2023-11-03' } -OutFile $package -TimeoutSec 120
if ((Get-FileHash $package -Algorithm SHA256).Hash -ne $PackageSha256) { throw 'The media package hash does not match the build.' }
$application = Join-Path $root 'app'
New-Item -ItemType Directory -Path $application -Force | Out-Null
Expand-Archive -Path $package -DestinationPath $application -Force
Remove-Item -LiteralPath $package

if (-not (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64' -ErrorAction SilentlyContinue).Installed) {
  $redistributable = Join-Path $root 'vc_redist.x64.exe'
  Invoke-WebRequest -UseBasicParsing -Uri 'https://aka.ms/vs/17/release/vc_redist.x64.exe' -OutFile $redistributable -TimeoutSec 120
  $signature = Get-AuthenticodeSignature $redistributable
  if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
    throw 'The Visual C++ runtime download is not signed by Microsoft.'
  }
  $install = Start-Process -FilePath $redistributable -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
  if ($install.ExitCode -notin @(0, 1638, 3010)) { throw "Visual C++ runtime install failed: $($install.ExitCode)" }
  Remove-Item -LiteralPath $redistributable
}

$settings = @{
  MediaBot = @{
    TenantId = $TenantId; AppId = $AppId; CertificateThumbprint = $certificate.Thumbprint
    ServiceFqdn = $Fqdn; PublicIp = $PublicIp; MediaPort = 8445
    ApiBase = $ApiBase; ApiKey = Get-MediaSecret 'media-control-key'; MaxConcurrentMeetings = 1
  }
  Logging = @{ LogLevel = @{ Default = 'Information'; 'Microsoft.AspNetCore' = 'Warning'; 'System.Net.Http.HttpClient' = 'Warning' } }
}
$settings | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $application 'appsettings.json') -Encoding UTF8
if (-not (Get-NetFirewallRule | Where-Object Name -eq 'ZavaMediaBot')) {
  New-NetFirewallRule -Name 'ZavaMediaBot' -DisplayName 'Zava media HTTPS and TCP media' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443, 8445 | Out-Null
}
$action = New-ScheduledTaskAction -Execute (Join-Path $application 'Zava.Scrum.MediaBot.exe') -WorkingDirectory $application
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$taskSettings = New-ScheduledTaskSettingsSet -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit ([TimeSpan]::Zero)
Register-ScheduledTask -TaskName 'ZavaMediaBot' -Action $action -Trigger $trigger -Principal $principal -Settings $taskSettings -Force | Out-Null
# Return only the public certificate. The calling application's keyCredentials never contain a private key.
Write-Output ('PUBLIC_CERT:' + [Convert]::ToBase64String($certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)))
Start-ScheduledTask -TaskName 'ZavaMediaBot'
