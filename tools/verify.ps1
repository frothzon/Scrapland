#Requires -Version 5.1
<#
.SYNOPSIS
    Full headless gate: build + unit tests + in-engine tests + asset audit.

.DESCRIPTION
    A milestone is only "done" when this script exits 0 (see agents.md).
    Nothing here opens a window; everything runs with --headless.

.EXAMPLE
    tools\verify.ps1

.EXAMPLE
    tools\verify.ps1 -SkipImport -Filter smoke
#>
[CmdletBinding()]
param(
    [string]$Filter = 'all',
    [switch]$SkipImport
)

. (Join-Path $PSScriptRoot 'common.ps1')

$failures = [System.Collections.Generic.List[string]]::new()

& (Join-Path $PSScriptRoot 'test.ps1') -Filter $Filter -SkipImport:$SkipImport
if ($LASTEXITCODE -ne 0) { $failures.Add('test.ps1') }

& (Join-Path $PSScriptRoot 'audit-assets.ps1')
if ($LASTEXITCODE -ne 0) { $failures.Add('audit-assets.ps1') }

Write-Step 'VERIFY RESULT'
if ($failures.Count -eq 0) {
    Write-Host 'VERIFY OK - all headless checks passed' -ForegroundColor Green
    exit 0
}

foreach ($failure in $failures) {
    Write-Host "FAILED: $failure" -ForegroundColor Red
}
exit 1
