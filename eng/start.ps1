[CmdletBinding()]
param(
    [switch]$Containerized
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'infra/compose.yaml'

Push-Location $repositoryRoot
try {
    if ($Containerized) {
        docker compose --file $composeFile up --build --detach --wait
        Write-Host 'Community Starter is available at http://localhost:8080'
        return
    }

    docker compose --file $composeFile up --detach --wait postgres minio mailpit clamav
    dotnet tool restore
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project backend/src/CommunityStarter.Api/CommunityStarter.Api.csproj --launch-profile https
}
finally {
    Pop-Location
}
