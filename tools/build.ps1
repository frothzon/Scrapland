#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the game assembly (which also builds Forager.Core).
#>
[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common.ps1')

Write-Step 'dotnet build (ForagerClone -> Forager.Core)'
& dotnet build (Join-Path (Get-GodotProjectPath) 'ForagerClone.csproj') -c Debug --nologo
exit $LASTEXITCODE
