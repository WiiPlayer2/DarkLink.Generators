#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

$now = [DateTime]::Now
$versionSuffix = "-pre$($now.ToString("yyyyMMddHHmmss"))"

New-Item ./output -ItemType Directory -Force | Out-Null
Remove-Item ./output/* -Recurse -Force | Out-Null
dotnet pack ./DarkLink.Generators/DarkLink.Generators.csproj `
    --configuration Release `
    --version-suffix $versionSuffix `
    --output ./output `
    --verbosity normal