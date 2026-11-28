# Pure declaration/grant reconciliation for the approved delegated knowledge permissions (ADR0046).
function Get-KnowledgeGraphPermissions {
  @(
    @{ id = 'df85f4d6-205c-4ac5-a5ea-6bf408dba283'; type = 'Scope' }
    @{ id = '205e70e5-aba6-4c52-a976-6d2d46c48043'; type = 'Scope' }
  )
}

function Merge-KnowledgeGraphConsent([string]$ExistingScopes, [bool]$Approved) {
  $scopes = [System.Collections.Generic.List[string]]::new()
  foreach ($scope in @($ExistingScopes -split '\s+') + @('User.Read', 'openid', 'profile')) {
    if ($scope -and -not $scopes.Contains($scope)) { $scopes.Add($scope) }
  }
  if ($Approved) {
    foreach ($scope in @('Files.Read.All', 'Sites.Read.All')) {
      if (-not $scopes.Contains($scope)) { $scopes.Add($scope) }
    }
  }
  $scopes -join ' '
}
