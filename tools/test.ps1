#Requires -Version 5.1
<#
.SYNOPSIS
    The single "run all tests headlessly" entry point.

.DESCRIPTION
    Tier 0: build the game assembly.
    Tier 1: dotnet test over tests\Forager.Tests (Forager.Core, no engine).
    Tier 2: in-engine headless suite (res://Tests/Headless/TestRunner.tscn) with a hard timeout.

    Exits non-zero if any tier fails, so it can gate CI and agent work.

.EXAMPLE
    tools\test.ps1

.EXAMPLE
    tools\test.ps1 -UnitOnly

.EXAMPLE
    tools\test.ps1 -EngineOnly -Filter smoke -SkipImport
#>
[CmdletBinding()]
param(
    [switch]$SkipImport,
    [switch]$UnitOnly,
    [switch]$EngineOnly,
    [string]$Filter = 'all',
    [int]$TimeoutSeconds = 300
)

. (Join-Path $PSScriptRoot 'common.ps1')

$artifacts = New-ArtifactsDirectory
$failures = [System.Collections.Generic.List[string]]::new()

if (-not $EngineOnly) {
    Write-Step 'Tier 0 + 1: build game assembly, then dotnet test (Forager.Core)'
    & dotnet build (Join-Path (Get-GodotProjectPath) 'ForagerClone.csproj') -c Debug --nologo
    if ($LASTEXITCODE -ne 0) { $failures.Add('tier0 build (ForagerClone)') }

    if ($failures.Count -eq 0) {
        & dotnet test (Join-Path (Get-RepoRoot) 'tests\Forager.Tests\Forager.Tests.csproj') -c Debug --nologo
        if ($LASTEXITCODE -ne 0) { $failures.Add('tier1 dotnet test (Forager.Core)') }
    }
}

if (-not $UnitOnly) {
    if (-not $SkipImport) {
        Write-Step 'Importing assets (headless)'
        $importCode = Invoke-Godot -Arguments @('--headless', '--path', (Get-GodotProjectPath), '--import')
        if ($importCode -ne 0) { $failures.Add("asset import (exit $importCode)") }
    }

    Write-Step "Tier 2: in-engine headless tests (filter=$Filter, timeout=${TimeoutSeconds}s)"
    $log = Join-Path $artifacts 'headless-tests.log'
    $engineArgs = @(
        '--headless',
        '--fixed-fps', '60',
        '--path', (Get-GodotProjectPath),
        '--scene', 'res://Tests/Headless/TestRunner.tscn',
        '--log-file', $log
    )
    $userArgs = @("--filter=$Filter", '--seed=1337', "--timeout=$TimeoutSeconds")

    $engineCode = Invoke-Godot -Arguments $engineArgs -UserArguments $userArgs
    switch ($engineCode) {
        0 { }
        1 { $failures.Add('tier2 headless test failures') }
        2 { $failures.Add('tier2 global timeout') }
        3 { $failures.Add('tier2 refused to run outside --headless') }
        4 { $failures.Add("tier2 no tests matched filter '$Filter'") }
        default { $failures.Add("tier2 unexpected exit code $engineCode") }
    }
}

Write-Step 'RESULT'
if ($failures.Count -eq 0) {
    Write-Host 'ALL HEADLESS CHECKS PASSED' -ForegroundColor Green
    exit 0
}

foreach ($failure in $failures) {
    Write-Host "FAILED: $failure" -ForegroundColor Red
}
Write-Host 'HEADLESS CHECKS FAILED' -ForegroundColor Red
exit 1
