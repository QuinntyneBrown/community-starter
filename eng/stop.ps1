[CmdletBinding()]
param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'infra/compose.yaml'
$arguments = @('compose', '--file', $composeFile, 'down', '--remove-orphans')
if ($RemoveVolumes) {
    $arguments += '--volumes'
    Write-Warning 'Removing local PostgreSQL, object-storage, and antivirus data volumes.'
}

& docker @arguments
