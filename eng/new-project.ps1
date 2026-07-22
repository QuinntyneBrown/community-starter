[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$DisplayName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$CodeName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath,

    [switch]$SkipQualityCheck
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$specializer = Join-Path $repositoryRoot 'scripts/specialize_project.py'
$arguments = @(
    $specializer,
    '--display-name', $DisplayName,
    '--code-name', $CodeName,
    '--output-path', $OutputPath
)
if ($SkipQualityCheck) {
    $arguments += '--skip-quality-check'
}

python @arguments
