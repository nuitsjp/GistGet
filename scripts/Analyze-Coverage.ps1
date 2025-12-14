<#
.SYNOPSIS
    Analyze cobertura coverage results and enforce a minimum threshold.

.PARAMETER ResultsDirectory
    Directory that contains coverage.cobertura.xml files. Defaults to ../TestResults.

.PARAMETER Threshold
    Minimum line coverage percentage required to pass. Set to 0 to skip enforcement.

.PARAMETER Top
    Number of lowest coverage files to display.

.PARAMETER ExcludePatterns
    Regex patterns for file paths to exclude from the coverage summary.
    Defaults exclude obj folders, generated *.g.cs files, Program.cs, and GitHubClientFactory.cs.
#>

param(
    [string]$ResultsDirectory = "$(Split-Path -Parent $PSScriptRoot)\TestResults",
    [double]$Threshold = 0,
    [int]$Top = 5,
    [string[]]$ExcludePatterns = @(
        '[\\/]obj[\\/]',
        '\.g\.cs$',
        'Program\.cs$',
        'GitHubClientFactory\.cs$'
    )
)

$ErrorActionPreference = "Stop"

function Get-LatestCoverageFile {
    param([string]$Root)

    $files = Get-ChildItem -Path $Root -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending
    if (-not $files) {
        throw "coverage.cobertura.xml が見つかりませんでした。まずテストを実行してカバレージを生成してください。"
    }

    return $files[0]
}

function Get-CoverageSummary {
    param(
        [string]$FilePath,
        [string[]]$ExcludePatterns
    )

    [xml]$xml = Get-Content -Path $FilePath
    $classes = $xml.coverage.packages.package.classes.class

    if ($ExcludePatterns -and $ExcludePatterns.Count -gt 0) {
        $classes = $classes | Where-Object {
            $file = $_.filename
            -not ($ExcludePatterns | Where-Object { $file -match $_ })
        }
    }

    $fileMap = @{}
    foreach ($cls in $classes) {
        $file = $cls.filename
        if (-not $fileMap.ContainsKey($file)) {
            $fileMap[$file] = [pscustomobject]@{
                File    = $file
                Covered = 0
                Total   = 0
            }
        }

        foreach ($line in $cls.lines.line) {
            $fileMap[$file].Total++
            if ([int]$line.hits -gt 0) {
                $fileMap[$file].Covered++
            }
        }
    }

    $files = $fileMap.Values | ForEach-Object {
        $coverage = if ($_.Total -eq 0) { 0 } else { ($_.Covered / $_.Total) * 100 }
        [pscustomobject]@{
            File     = $_.File
            Covered  = $_.Covered
            Total    = $_.Total
            Coverage = $coverage
        }
    }

    $coveredTotal = ($files | Measure-Object -Property Covered -Sum).Sum
    $linesTotal = ($files | Measure-Object -Property Total -Sum).Sum
    $lineCoverage = if ($linesTotal -eq 0) { 0 } else { ($coveredTotal / $linesTotal) * 100 }

    return [pscustomobject]@{
        FilePath      = $FilePath
        LineCoverage  = $lineCoverage
        BranchCoverage = [double]$xml.coverage.'branch-rate' * 100
        Files         = $files
        CoveredLines  = $coveredTotal
        TotalLines    = $linesTotal
    }
}

$coverageFile = Get-LatestCoverageFile -Root $ResultsDirectory
$summary = Get-CoverageSummary -FilePath $coverageFile.FullName -ExcludePatterns $ExcludePatterns

Write-Host "Coverage file: $($coverageFile.FullName)" -ForegroundColor Gray
Write-Host ("Line coverage : {0:N2}% ({1}/{2} lines)" -f $summary.LineCoverage, $summary.CoveredLines, $summary.TotalLines) -ForegroundColor Cyan
Write-Host ("Branch coverage: {0:N2}%" -f $summary.BranchCoverage) -ForegroundColor Cyan

if ($ExcludePatterns -and $ExcludePatterns.Count -gt 0) {
    Write-Host "Excluded patterns: $($ExcludePatterns -join ', ')" -ForegroundColor Gray
}

if ($Top -gt 0) {
    Write-Host ""
    Write-Host "Coverage (lowest $Top files):" -ForegroundColor Yellow
    $summary.Files
        | Sort-Object Coverage
        | Select-Object -First $Top
        | ForEach-Object {
            Write-Host ("  {0,-70} {1,6:N2}% ({2}/{3})" -f $_.File, $_.Coverage, $_.Covered, $_.Total)
        }
}

if ($Threshold -gt 0) {
    if ($summary.LineCoverage -lt $Threshold) {
        Write-Host ("Coverage threshold not met: {0:N2}% < {1:N2}%" -f $summary.LineCoverage, $Threshold) -ForegroundColor Red
        exit 1
    }

    Write-Host ("Coverage threshold met: {0:N2}% >= {1:N2}%" -f $summary.LineCoverage, $Threshold) -ForegroundColor Green
}
