#Requires -Version 5.0
# RUN_TESTS_SET1.ps1 - Navy Phase 1 test runner
# Usage: powershell -ExecutionPolicy Bypass -File RUN_TESTS_SET1.ps1

$ErrorActionPreference = 'Stop'

# --- Helpers -----------------------------------------------------------------

function Write-Color {
    param([string]$Text, [string]$Color = 'White')
    Write-Host $Text -ForegroundColor $Color
}

function Print-Banner {
    $dotnetVer = ''
    try { $dotnetVer = & "$script:dotnetExe" --version 2>&1 } catch {}
    Write-Color ''
    Write-Color '===========================================================' Cyan
    Write-Color '  Navy (Battleship) - Phase 1 Test Suite' Cyan
    Write-Color "  Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" Cyan
    Write-Color "  .NET SDK: $dotnetVer" Cyan
    Write-Color '===========================================================' Cyan
    Write-Color ''
}

# --- Find dotnet -------------------------------------------------------------

$dotnetExe = $null
$candidatePaths = @(
    'C:\Program Files\dotnet\dotnet.exe',
    'C:\Program Files (x86)\dotnet\dotnet.exe'
)
foreach ($p in $candidatePaths) {
    if (Test-Path $p) { $dotnetExe = $p; break }
}
if (-not $dotnetExe) {
    $found = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($found) { $dotnetExe = $found.Source }
}
if (-not $dotnetExe) {
    Write-Color '[ERROR] dotnet.exe not found. Install .NET 8 SDK from https://dotnet.microsoft.com' Red
    Read-Host -Prompt 'Press ENTER to exit'
    exit 1
}

# --- Check version -----------------------------------------------------------

$ErrorActionPreference = 'Continue'
$versionOutput = & "$dotnetExe" --version 2>&1
$ErrorActionPreference = 'Stop'
if ($LASTEXITCODE -ne 0 -or -not ($versionOutput -match '^8\.')) {
    Write-Color "[ERROR] .NET 8.x SDK required. Found: $versionOutput" Red
    Read-Host -Prompt 'Press ENTER to exit'
    exit 1
}

# --- Paths -------------------------------------------------------------------

$scriptDir   = $PSScriptRoot
$slnPath     = Join-Path $scriptDir 'Navy.Tests.sln'
$coverageDir = Join-Path $scriptDir 'coverage'

Print-Banner

# --- Clean coverage output ---------------------------------------------------

if (Test-Path $coverageDir) {
    Remove-Item $coverageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $coverageDir | Out-Null

# --- Restore -----------------------------------------------------------------

Write-Color '[1/4] Restoring NuGet packages...' Yellow
$ErrorActionPreference = 'Continue'
& "$dotnetExe" restore "$slnPath"
$restoreExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($restoreExit -ne 0) {
    Write-Color '[ERROR] Restore failed.' Red
    Read-Host -Prompt 'Press ENTER to exit'
    exit 1
}

# --- Build -------------------------------------------------------------------

Write-Color ''
Write-Color '[2/4] Building (Release)...' Yellow
$ErrorActionPreference = 'Continue'
& "$dotnetExe" build "$slnPath" -c Release --nologo
$buildExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($buildExit -ne 0) {
    Write-Color '[ERROR] Build failed.' Red
    Read-Host -Prompt 'Press ENTER to exit'
    exit 1
}

# --- Test --------------------------------------------------------------------

Write-Color ''
Write-Color '[3/4] Running tests + collecting coverage...' Yellow

$ErrorActionPreference = 'Continue'
$testLines = & "$dotnetExe" test "$slnPath" -c Release --no-build --nologo `
    --logger "console;verbosity=normal" `
    --collect:"XPlat Code Coverage" `
    --results-directory "$coverageDir" 2>&1
$testExitCode = $LASTEXITCODE
$ErrorActionPreference = 'Stop'

# Echo lines to console
$testLines | ForEach-Object { Write-Host $_ }

# --- Parse test summary ------------------------------------------------------

$totalPassed  = 0
$totalFailed  = 0
$totalSkipped = 0
$totalTime    = '?'

foreach ($line in $testLines) {
    # Format 1: "Failed: 0, Passed: 108, Skipped: 0, Total: 108, Duration: 213 ms"
    if ($line -match 'Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)') {
        $totalFailed  += [int]$Matches[1]
        $totalPassed  += [int]$Matches[2]
        $totalSkipped += [int]$Matches[3]
    }
    # Format 2: individual lines like "     Passed: 108"
    if ($line -match '^\s+Passed:\s+(\d+)\s*$') {
        $totalPassed += [int]$Matches[1]
    }
    if ($line -match '^\s+Failed:\s+(\d+)\s*$') {
        $totalFailed += [int]$Matches[1]
    }
    if ($line -match '^\s+Skipped:\s+(\d+)\s*$') {
        $totalSkipped += [int]$Matches[1]
    }
    if ($line -match 'Total time:\s*([\d\.]+\s*\w+)') {
        $totalTime = $Matches[1]
    }
}

# --- Parse coverage ----------------------------------------------------------

Write-Color ''
Write-Color '[4/4] Coverage summary...' Yellow
Write-Color ''

$coverageFiles = Get-ChildItem -Path $coverageDir -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue

# classGroups: name -> @{ lv=int; lc=int }
# We take the best (highest coverage) seen for each class across all XML files.
$classGroups = @{}

foreach ($f in $coverageFiles) {
    [xml]$xml = Get-Content $f.FullName
    foreach ($pkg in $xml.coverage.packages.package) {
        foreach ($cls in $pkg.classes.class) {
            if ($null -eq $cls -or $null -eq $cls.name) { continue }
            $name = $cls.name
            if ($name -match '^Cysharp') { continue }

            # Count lines by iterating <line> elements inside methods
            $lv = 0; $lc = 0
            foreach ($m in $cls.methods.method) {
                foreach ($line in $m.lines.line) {
                    $lv++
                    if ([int]$line.hits -gt 0) { $lc++ }
                }
            }
            if ($lv -eq 0) { continue }

            $existing = $classGroups[$name]
            if ($null -eq $existing -or $lc -gt $existing.lc) {
                $classGroups[$name] = @{ lv = $lv; lc = $lc }
            }
        }
    }
}

$tableHeader = 'Assembly'.PadRight(45) + 'Lines'.PadLeft(7) + 'Covered'.PadLeft(9) + '   %'.PadLeft(6)
Write-Color $tableHeader White
Write-Color ('-' * 69) DarkGray

$coreCovered = 0
$coreValid   = 0
$dtoCovered  = 0
$dtoValid    = 0

foreach ($entry in ($classGroups.GetEnumerator() | Sort-Object Key)) {
    $name  = $entry.Key
    $data  = $entry.Value
    $pct   = if ($data.lv -gt 0) { $data.lc / $data.lv * 100 } else { 0 }
    $color = if ($pct -ge 90) { 'Green' } elseif ($pct -ge 80) { 'Yellow' } else { 'Red' }
    $row   = $name.PadRight(45) + $data.lv.ToString().PadLeft(7) + $data.lc.ToString().PadLeft(9) + ("{0:F1}" -f $pct).PadLeft(6) + ' %'
    Write-Color $row $color

    if ($name -match '^Navy\.Core\.(Models|Engine)') {
        $coreCovered += $data.lc; $coreValid += $data.lv
    }
    if ($name -match '^Navy\.Data\.Firebase\.Dto') {
        $dtoCovered += $data.lc; $dtoValid += $data.lv
    }
}

Write-Color ('-' * 69) DarkGray

$coverageOk = $true

$corePct   = if ($coreValid -gt 0) { $coreCovered / $coreValid * 100 } else { 0 }
$coreColor = if ($corePct -ge 95) { 'Green' } elseif ($corePct -ge 80) { 'Yellow' } else { 'Red' }
Write-Color ("Navy.Core (target assemblies)".PadRight(45) + $coreValid.ToString().PadLeft(7) + $coreCovered.ToString().PadLeft(9) + ("{0:F1}" -f $corePct).PadLeft(6) + ' %  [target: 95%]') $coreColor
if ($corePct -lt 95) { $coverageOk = $false }

if ($dtoValid -gt 0) {
    $dtoPct   = $dtoCovered / $dtoValid * 100
    $dtoColor = if ($dtoPct -ge 95) { 'Green' } elseif ($dtoPct -ge 80) { 'Yellow' } else { 'Red' }
    Write-Color ("Navy.Data.Firebase.Dto".PadRight(45) + $dtoValid.ToString().PadLeft(7) + $dtoCovered.ToString().PadLeft(9) + ("{0:F1}" -f $dtoPct).PadLeft(6) + ' %  [target: 95%]') $dtoColor
    if ($dtoPct -lt 95) { $coverageOk = $false }
}

Write-Color ''

# --- Test result summary -----------------------------------------------------

$testColor = if ($totalFailed -eq 0) { 'Green' } else { 'Red' }
Write-Color '----------------------------------------------------------' DarkGray
Write-Color ("Passed: {0} | Failed: {1} | Skipped: {2} | Duration: {3}" -f $totalPassed, $totalFailed, $totalSkipped, $totalTime) $testColor
Write-Color ''

$finalOk = ($testExitCode -eq 0) -and $coverageOk

if ($finalOk) {
    Write-Color 'ALL CHECKS PASSED' Green
} else {
    if ($testExitCode -ne 0 -or $totalFailed -gt 0) {
        Write-Color 'SOME TESTS FAILED' Red
    }
    if (-not $coverageOk) {
        Write-Color 'COVERAGE BELOW TARGET' Red
    }
}

Write-Color ''
Read-Host -Prompt 'Press ENTER to exit'

if (-not $finalOk) { exit 1 }
exit 0
