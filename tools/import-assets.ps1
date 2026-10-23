#Requires -Version 5.1
<#
.SYNOPSIS
    Imports all assets headlessly (creates/refreshes the .godot cache). Required once on a
    fresh checkout before any headless run, because Godot cannot load models that were never
    imported (see lessons_learned.md LL-002).
#>
[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common.ps1')

Write-Step 'Godot headless import'
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$code = Invoke-Godot -Arguments @('--headless', '--path', (Get-GodotProjectPath), '--import')
$stopwatch.Stop()
Write-Host ("Import finished in {0:N1}s with exit code {1}" -f $stopwatch.Elapsed.TotalSeconds, $code) -ForegroundColor ($(if ($code -eq 0) { 'Green' } else { 'Red' }))
exit $code
