#Requires -Version 5.1
<#
.SYNOPSIS
    Shared helpers for the headless toolchain (dot-sourced by the other tools\*.ps1 scripts).

.NOTES
    Paths contain spaces ("Forager Clone"), so every native invocation goes through an
    argument array and is quoted by PowerShell - never build command strings by hand.
#>

$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:GodotProjectName = 'Forager Clone Project'
$script:GodotProjectPath = Join-Path $script:RepoRoot $script:GodotProjectName
$script:ArtifactsPath = Join-Path $script:RepoRoot 'artifacts'

$script:DefaultGodotExe = Join-Path $script:RepoRoot 'Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
$script:GodotExe = if ($env:GODOT_BIN) { $env:GODOT_BIN } else { $script:DefaultGodotExe }

function Get-RepoRoot { $script:RepoRoot }
function Get-GodotProjectPath { $script:GodotProjectPath }
function Get-ArtifactsPath { $script:ArtifactsPath }

function Assert-GodotExe {
    if (-not (Test-Path -LiteralPath $script:GodotExe)) {
        throw "Godot mono editor binary not found: $($script:GodotExe). Set `$env:GODOT_BIN to override."
    }
}

function New-ArtifactsDirectory {
    if (-not (Test-Path -LiteralPath $script:ArtifactsPath)) {
        New-Item -ItemType Directory -Path $script:ArtifactsPath | Out-Null
    }
    return $script:ArtifactsPath
}

<#
.SYNOPSIS
    Runs the Godot mono editor binary headlessly and returns its exit code.
.DESCRIPTION
    -Arguments are engine args (before `--`), -UserArguments land after `--` and are what
    OS.GetCmdlineUserArgs() sees inside the game/tests.
#>
function Invoke-Godot {
    param(
        [string[]]$Arguments = @(),
        [string[]]$UserArguments = @()
    )

    Assert-GodotExe
    $all = @($Arguments)
    if ($UserArguments.Count -gt 0) {
        $all += '--'
        $all += $UserArguments
    }

    # Capture, then write to host: PowerShell functions otherwise return the native stdout
    # as part of their value and the exit code gets lost in the array (lessons_learned.md LL-009).
    # Flattening to strings keeps engine stderr from being rendered as RemoteException blocks.
    $output = & $script:GodotExe @all 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    if ($output) {
        $output | Out-Host
    }

    return [int]$code
}

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host "=== $Message" -ForegroundColor Cyan
}
