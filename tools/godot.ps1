#Requires -Version 5.1
<#
.SYNOPSIS
    Thin wrapper around the Godot 4.7.2 mono editor binary.

.EXAMPLE
    tools\godot.ps1 -Arguments '--headless','--path','Forager Clone Project','--import'

.EXAMPLE
    # Run the headless suite with extra user args (everything after `--`):
    tools\godot.ps1 -Arguments '--headless','--path','Forager Clone Project','--scene','res://Tests/Headless/TestRunner.tscn' -UserArguments '--filter=smoke'

.NOTES
    Named parameters are used instead of $args because PowerShell 5.1 swallows the literal
    `--` separator in remaining-argument binding. See lessons_learned.md LL-012.
#>
[CmdletBinding()]
param(
    [string[]]$Arguments = @(),
    [string[]]$UserArguments = @()
)

. (Join-Path $PSScriptRoot 'common.ps1')

$code = Invoke-Godot -Arguments $Arguments -UserArguments $UserArguments
exit $code
