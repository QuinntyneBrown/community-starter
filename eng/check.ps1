[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repositoryRoot
try {
    python -m unittest discover -s scripts/tests -p 'test_*.py'
    python scripts/verify_detailed_designs.py
    python scripts/generate_feature_contracts.py
    dotnet restore backend/CommunityStarter.sln --locked-mode
    dotnet build backend/CommunityStarter.sln --configuration Release --no-restore
    dotnet test backend/CommunityStarter.sln --configuration Release --no-build
    dotnet list backend/CommunityStarter.sln package --vulnerable --include-transitive
    npx --yes --package node@24.18.0 --package npm@11.6.2 npm ci
    npx --yes --package node@24.18.0 --package npm@11.6.2 npm run format:check
    npx --yes --package node@24.18.0 --package npm@11.6.2 npm run test
    npx --yes --package node@24.18.0 --package npm@11.6.2 npm run build
    npx --yes --package node@24.18.0 --package npm@11.6.2 npm audit --audit-level=high
}
finally {
    Pop-Location
}
