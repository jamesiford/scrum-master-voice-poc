<#
.SYNOPSIS
Stages, explicitly approves, resumes Azure DevOps reset, then publishes the atomic local film baseline.
.DESCRIPTION
Uses only the Admin-protected API and existing approved follow-up apply flow. Never invokes SeedAdo,
deletes data directly, infers demo teams from names, or records access tokens. The allowlist and persisted
isDemo/demoTenantId markers must already be explicitly configured. Legacy unmarked teams are refused.
Azure DevOps is a recoverable per-item phase; it is not distributed ACID with the local publication.
.PARAMETER ApiUrl
API origin; defaults to API_URI.
.PARAMETER TeamId
Exact demo target. All other explicitly allowlisted and demo-marked teams are cleaned as debris at commit.
.PARAMETER PortalClientId
Portal application id; defaults to PORTAL_CLIENT_ID.
.PARAMETER OperationId
Optional journal id from an interrupted run. Omit to stage or resume the current operation.
.PARAMETER Approve
Explicit approval of the staged item baseline. Without it, the script stages and prints the recovery contract only.
.PARAMETER Abort
Explicitly releases an undecided or rejected operation. Approved/partially applied operations must be resumed.
#>
[CmdletBinding()]
param(
    [string]$ApiUrl = $env:API_URI,
    [Parameter(Mandatory)][string]$TeamId,
    [string]$PortalClientId = $env:PORTAL_CLIENT_ID,
    [guid]$OperationId = [guid]::Empty,
    [switch]$Approve,
    [switch]$Abort
)
$ErrorActionPreference = 'Stop'
if (-not $ApiUrl -or -not $PortalClientId) { throw 'ApiUrl and PortalClientId are required.' }
if ($Approve -and $Abort) { throw 'Choose either Approve or Abort.' }
if ($Abort -and $OperationId -eq [guid]::Empty) { throw 'Abort requires an existing OperationId.' }
$origin = [uri]$ApiUrl
if ($origin.Scheme -ne 'https' -and -not $origin.IsLoopback) { throw 'Use HTTPS or a loopback API.' }
$token = az account get-access-token --scope "api://$PortalClientId/access_as_user" --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or -not $token) { throw 'An Admin portal access token is required.' }
$headers = @{ Authorization = "Bearer $token" }
$base = $ApiUrl.TrimEnd('/')
$scope = "?teamId=$([uri]::EscapeDataString($TeamId))"
try {
    if ($OperationId -eq [guid]::Empty) {
        $OperationId = [guid](Invoke-RestMethod -Method Post -Uri "$base/api/demo/reseed$scope" -Headers $headers)
    }
    $operation = Invoke-RestMethod -Uri "$base/api/demo/reseed/$OperationId$scope" -Headers $headers
    if ($Abort) {
        Invoke-RestMethod -Method Post -Uri "$base/api/demo/reseed/$OperationId/abort$scope" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$base/api/demo/reseed/$OperationId$scope" -Headers $headers | ConvertTo-Json -Depth 4
        return
    }
    if ($operation.state -eq 'Completed' -or -not $Approve) {
        $operation | ConvertTo-Json -Depth 4
        return
    }
    $followUp = Invoke-RestMethod -Uri "$base/api/followups/$($operation.followUpId)" -Headers $headers
    if ($followUp.status -in @('Pending', 'Sent')) {
        Invoke-RestMethod -Method Post -Uri "$base/api/followups/$($operation.followUpId)/approve" -Headers $headers | Out-Null
    }
    Invoke-RestMethod -Method Post -Uri "$base/api/followups/$($operation.followUpId)/apply" -Headers $headers | Out-Null
    Invoke-RestMethod -Method Post -Uri "$base/api/demo/reseed/$OperationId/commit$scope" -Headers $headers | Out-Null
    Invoke-RestMethod -Uri "$base/api/demo/reseed/$OperationId$scope" -Headers $headers | ConvertTo-Json -Depth 4
} finally {
    Remove-Variable token, headers -ErrorAction SilentlyContinue
}
