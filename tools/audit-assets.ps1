#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the headless asset audit: loads every model/texture under res://Assets and prints a
    report used to fill Docs\asset_inventory.md.
#>
[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common.ps1')

$artifacts = New-ArtifactsDirectory
$log = Join-Path $artifacts 'asset-audit.log'

Write-Step 'Headless asset audit'
$code = Invoke-Godot -Arguments @(
    '--headless',
    '--path', (Get-GodotProjectPath),
    '--scene', 'res://Tests/Headless/AssetAudit.tscn',
    '--log-file', $log
)

if (Test-Path -LiteralPath $log) {
    Write-Host ''
    Select-String -Path $log -Pattern 'MODEL\||\[audit\]' | ForEach-Object { Write-Host $_.Line }
}

Write-Host ''
Write-Host "Asset audit exit code: $code" -ForegroundColor ($(if ($code -eq 0) { 'Green' } else { 'Red' }))
exit $code
