# Targeted pure tests. No Azure/Graph requests, identity changes or source retrieval.
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\..\infra\hooks\lib.ps1"
. "$PSScriptRoot\..\infra\hooks\knowledge-consent.ps1"
$permissions = @(Get-KnowledgeGraphPermissions)
if ($permissions.Count -ne 2 -or @($permissions | Where-Object type -ne Scope).Count) {
  throw 'Declaration_Knowledge_OnlyTwoDelegatedScopes failed'
}
if ($permissions[0].id -ne 'df85f4d6-205c-4ac5-a5ea-6bf408dba283' -or
  $permissions[1].id -ne '205e70e5-aba6-4c52-a976-6d2d46c48043') {
  throw 'Declaration_Knowledge_MatchesVerifiedGraphMetadata failed'
}
$existing = @(@{ resourceAppId = 'graph'; resourceAccess = @(
  @{ id = $permissions[0].id; type = 'Scope' }
  @{ id = 'other-approved'; type = 'Scope' }) })
$required = @(@{ resourceAppId = 'graph'; resourceAccess = $permissions })
$first = @(Merge-RequiredResourceAccess $existing $required)
$second = @(Merge-RequiredResourceAccess $first $required)
if ($first[0].resourceAccess.Count -ne 3 -or
  ($first | ConvertTo-Json -Depth 10 -Compress) -ne ($second | ConvertTo-Json -Depth 10 -Compress)) {
  throw 'Declaration_RepeatedRun_PreservesExistingAndDeduplicates failed'
}
$off = Merge-KnowledgeGraphConsent '' $false
if ($off -ne 'User.Read openid profile') { throw 'Consent_NotApproved_NoKnowledgeGrant failed' }
$approved = Merge-KnowledgeGraphConsent $off $true
if ($approved -ne 'User.Read openid profile Files.Read.All Sites.Read.All') { throw 'Consent_Approved_AddsBothDelegatedScopes failed' }
$again = Merge-KnowledgeGraphConsent $approved $true
$disabled = Merge-KnowledgeGraphConsent $approved $false
if ($again -ne $approved -or $disabled -ne $approved) { throw 'Consent_ExistingApproval_PreservedIdempotently failed' }
$other = Merge-KnowledgeGraphConsent 'Other.Approved.Scope User.Read openid profile' $true
if (-not $other.StartsWith('Other.Approved.Scope ')) { throw 'Consent_SeparateApprovedScope_Preserved failed' }
foreach ($path in @('knowledge-consent.ps1', 'entra-portal-app.ps1')) {
  $errors = $null
  [System.Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\..\infra\hooks\$path", [ref]$null, [ref]$errors) | Out-Null
  if ($errors.Count) { throw "Parser_$path failed: $errors" }
}
Write-Host 'PASS knowledge consent: 7 pure cases and 2 real hook parsers; no external calls.'
