#!/usr/bin/env pwsh
$now = [DateTime]::Now
$versionSuffix = "-pre$($now.ToString("yyyyMMddHHmmss"))"
dotnet pack ./DarkLink.Generators/DarkLink.Generators.csproj --version-suffix $versionSuffix
