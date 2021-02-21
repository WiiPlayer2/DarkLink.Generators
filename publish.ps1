#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

Set-Location ./output
dotnet nuget push `
    '*.nupkg' `
    --api-key $env:NUGET_API_KEY `
    --source $env:NUGET_SOURCE
