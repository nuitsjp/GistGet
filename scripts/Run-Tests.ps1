<#
.SYNOPSIS
    GistGet プロジェクトのテスト、カバレッジ分析、メトリクス収集を実行します。

.DESCRIPTION
    このスクリプトは以下を順番に実行します:
    1. ソリューションのビルド
    2. テスト実行（カバレッジ収集込み）
    3. カバレッジ分析と閾値チェック
    4. コードメトリクス収集とレポート生成

.PARAMETER Configuration
    ビルド構成 (Debug または Release)。既定値は Debug。

.PARAMETER CoverageThreshold
    最低限必要なカバレッジ率 (%)。既定値は 95。0 を指定すると閾値チェックをスキップ。

.PARAMETER Top
    カバレッジが低いファイルの表示数。既定値は 5。

.PARAMETER MetricsOutputPath
    メトリクスレポートの出力パス。既定値は metrics-report.txt。

.PARAMETER MetricsFormat
    メトリクスレポートの出力形式 (Text または Json)。既定値は Text。

.EXAMPLE
    .\Run-Tests.ps1
    既定の設定でテスト、カバレッジ分析、メトリクス収集を実行

.EXAMPLE
    .\Run-Tests.ps1 -Configuration Release -CoverageThreshold 90
    Release ビルドで実行し、カバレッジ閾値を 90% に設定
#>

param(
    [string]$Configuration = "Debug",
    [double]$CoverageThreshold = 95,
    [int]$Top = 5,
    [string]$MetricsOutputPath = "metrics-report.txt",
    [ValidateSet('Text', 'Json')]
    [string]$MetricsFormat = "Text",
    [string[]]$ExcludePatterns = @(
        '[\\/]obj[\\/]',
        '\.g\.cs$',
        'Program\.cs$',
        'GitHubClientFactory\.cs$'
    )
)

$ErrorActionPreference = "Stop"

#region Common Utilities
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$runSettingsPath = Join-Path $repoRoot "coverlet.runsettings"
$platform = "x64"

function Write-Banner {
    param([string]$Title)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Test-LastExitCode {
    param([string]$Message)
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "$Message" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
#endregion

#region Coverage Functions
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
#endregion

#region Metrics Functions
function Get-LineCount {
    param([string[]]$Files)
    
    $totalLines = 0
    $codeLines = 0
    $commentLines = 0
    $blankLines = 0
    
    foreach ($file in $Files) {
        $content = Get-Content $file -ErrorAction SilentlyContinue
        if ($content) {
            $totalLines += $content.Count
            
            foreach ($line in $content) {
                $trimmed = $line.Trim()
                if ($trimmed -eq "") {
                    $blankLines++
                }
                elseif ($trimmed.StartsWith("//") -or $trimmed.StartsWith("/*") -or $trimmed.StartsWith("*")) {
                    $commentLines++
                }
                else {
                    $codeLines++
                }
            }
        }
    }
    
    return @{
        Total = $totalLines
        Code = $codeLines
        Comments = $commentLines
        Blank = $blankLines
    }
}

function Get-CodeQualityMetrics {
    param([string[]]$CsFiles)
    
    $totalClasses = 0
    $totalInterfaces = 0
    $totalMethods = 0
    $totalProperties = 0
    $totalComplexity = 0
    $maxComplexity = 0
    $maxComplexityMethod = ""
    
    foreach ($file in $CsFiles) {
        $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
        if (-not $content) { continue }
        
        # Count classes (excluding comments)
        $classMatches = [regex]::Matches($content, '\bclass\s+\w+')
        $totalClasses += $classMatches.Count
        
        # Count interfaces
        $interfaceMatches = [regex]::Matches($content, '\binterface\s+\w+')
        $totalInterfaces += $interfaceMatches.Count
        
        # Count methods (public, private, protected, internal)
        $methodMatches = [regex]::Matches($content, '(public|private|protected|internal|static)\s+[\w<>\[\]]+\s+\w+\s*\(')
        $totalMethods += $methodMatches.Count
        
        # Count properties
        $propertyMatches = [regex]::Matches($content, '(public|private|protected|internal)\s+[\w<>\[\]]+\s+\w+\s*\{\s*(get|set)')
        $totalProperties += $propertyMatches.Count
        
        # Calculate cyclomatic complexity (simplified)
        $lines = Get-Content $file
        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("/*") -or $trimmed.StartsWith("*")) {
                continue
            }
            
            $complexity = 0
            $complexity += ([regex]::Matches($trimmed, '\bif\s*\(')).Count
            $complexity += ([regex]::Matches($trimmed, '\belse\b')).Count
            $complexity += ([regex]::Matches($trimmed, '\bwhile\s*\(')).Count
            $complexity += ([regex]::Matches($trimmed, '\bfor\s*\(')).Count
            $complexity += ([regex]::Matches($trimmed, '\bforeach\s*\(')).Count
            $complexity += ([regex]::Matches($trimmed, '\bcase\s+')).Count
            $complexity += ([regex]::Matches($trimmed, '\bcatch\s*[\(\{]')).Count
            $complexity += ([regex]::Matches($trimmed, '\&\&')).Count
            $complexity += ([regex]::Matches($trimmed, '\|\|')).Count
            $complexity += ([regex]::Matches($trimmed, '\?')).Count - ([regex]::Matches($trimmed, '\?\?')).Count
            
            $totalComplexity += $complexity
            
            if ($complexity -gt $maxComplexity) {
                $maxComplexity = $complexity
                $maxComplexityMethod = Split-Path -Leaf $file
            }
        }
    }
    
    return @{
        Classes = $totalClasses
        Interfaces = $totalInterfaces
        Methods = $totalMethods
        Properties = $totalProperties
        TotalComplexity = $totalComplexity
        AverageComplexity = if ($totalMethods -gt 0) { [math]::Round($totalComplexity / $totalMethods, 2) } else { 0 }
        MaxComplexity = $maxComplexity
        MaxComplexityLocation = $maxComplexityMethod
    }
}
#endregion

#region Main Execution

Write-Banner "GistGet Test Pipeline"
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Coverage Threshold: $CoverageThreshold%" -ForegroundColor Yellow
Write-Host "Platform: $platform" -ForegroundColor Yellow

# Step 1: Build
Write-Banner "Step 1: Building Solution"
dotnet build "$repoRoot\src\GistGet.slnx" -c $Configuration -p:Platform=$platform
Test-LastExitCode "Build failed!"

# Step 2: Test
Write-Banner "Step 2: Running Tests"

$testArgs = @(
    "test",
    "$repoRoot\src\GistGet.Test\GistGet.Test.csproj",
    "-c", $Configuration,
    "--no-build",
    "-p:Platform=$platform",
    "--verbosity", "normal",
    "--collect", "XPlat Code Coverage",
    "--results-directory", "$repoRoot\TestResults",
    "--settings", $runSettingsPath
)

& dotnet $testArgs
Test-LastExitCode "Tests failed!"

# Step 3: Coverage Analysis
Write-Banner "Step 3: Analyzing Coverage"

$coverageFile = Get-LatestCoverageFile -Root "$repoRoot\TestResults"
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

if ($CoverageThreshold -gt 0) {
    if ($summary.LineCoverage -lt $CoverageThreshold) {
        Write-Host ("Coverage threshold not met: {0:N2}% < {1:N2}%" -f $summary.LineCoverage, $CoverageThreshold) -ForegroundColor Red
        exit 1
    }

    Write-Host ("Coverage threshold met: {0:N2}% >= {1:N2}%" -f $summary.LineCoverage, $CoverageThreshold) -ForegroundColor Green
}

# Step 4: Metrics Collection
Write-Banner "Step 4: Collecting Metrics"

$csFiles = Get-ChildItem -Path "$repoRoot\src" -Filter "*.cs" -Recurse -File
$csprojFiles = Get-ChildItem -Path "$repoRoot\src" -Filter "*.csproj" -Recurse -File
$yamlFiles = Get-ChildItem -Path "$repoRoot" -Filter "*.yaml" -Recurse -File
$ymlFiles = Get-ChildItem -Path "$repoRoot" -Filter "*.yml" -Recurse -File
$mdFiles = Get-ChildItem -Path "$repoRoot" -Filter "*.md" -Recurse -File
$ps1Files = Get-ChildItem -Path "$repoRoot" -Filter "*.ps1" -Recurse -File

$csMetrics = Get-LineCount -Files $csFiles.FullName
$ps1Metrics = Get-LineCount -Files $ps1Files.FullName
$qualityMetrics = Get-CodeQualityMetrics -CsFiles $csFiles.FullName

$metrics = @{
    CollectionDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Repository = "GistGet"
    Files = @{
        CSharp = $csFiles.Count
        Projects = $csprojFiles.Count
        YAML = $yamlFiles.Count + $ymlFiles.Count
        Markdown = $mdFiles.Count
        PowerShell = $ps1Files.Count
    }
    CSharpCode = @{
        TotalLines = $csMetrics.Total
        CodeLines = $csMetrics.Code
        CommentLines = $csMetrics.Comments
        BlankLines = $csMetrics.Blank
    }
    PowerShellCode = @{
        TotalLines = $ps1Metrics.Total
        CodeLines = $ps1Metrics.Code
        CommentLines = $ps1Metrics.Comments
        BlankLines = $ps1Metrics.Blank
    }
    CodeQuality = @{
        Classes = $qualityMetrics.Classes
        Interfaces = $qualityMetrics.Interfaces
        Methods = $qualityMetrics.Methods
        Properties = $qualityMetrics.Properties
        CyclomaticComplexity = $qualityMetrics.TotalComplexity
        AverageComplexity = $qualityMetrics.AverageComplexity
        MaxComplexity = $qualityMetrics.MaxComplexity
        MaxComplexityLocation = $qualityMetrics.MaxComplexityLocation
    }
    Projects = @()
}

foreach ($proj in $csprojFiles) {
    $projDir = Split-Path -Parent $proj.FullName
    $projName = $proj.BaseName
    $projCsFiles = Get-ChildItem -Path $projDir -Filter "*.cs" -Recurse -File
    
    $metrics.Projects += @{
        Name = $projName
        Path = $proj.FullName.Replace($repoRoot, "").TrimStart("\")
        Files = $projCsFiles.Count
    }
}

# Resolve output path
if (-not [System.IO.Path]::IsPathRooted($MetricsOutputPath)) {
    $MetricsOutputPath = Join-Path (Get-Location) $MetricsOutputPath
}

# Generate report
if ($MetricsFormat -eq "Json") {
    $metrics | ConvertTo-Json -Depth 10 | Out-File -FilePath $MetricsOutputPath -Encoding UTF8
}
else {
    $report = @"
========================================
GistGet Code Metrics Report
========================================
Collection Date: $($metrics.CollectionDate)

FILE STATISTICS
----------------------------------------
C# Files:          $($metrics.Files.CSharp)
Project Files:     $($metrics.Files.Projects)
YAML Files:        $($metrics.Files.YAML)
Markdown Files:    $($metrics.Files.Markdown)
PowerShell Files:  $($metrics.Files.PowerShell)

C# CODE METRICS
----------------------------------------
Total Lines:       $($metrics.CSharpCode.TotalLines)
Code Lines:        $($metrics.CSharpCode.CodeLines)
Comment Lines:     $($metrics.CSharpCode.CommentLines)
Blank Lines:       $($metrics.CSharpCode.BlankLines)

POWERSHELL CODE METRICS
----------------------------------------
Total Lines:       $($metrics.PowerShellCode.TotalLines)
Code Lines:        $($metrics.PowerShellCode.CodeLines)
Comment Lines:     $($metrics.PowerShellCode.CommentLines)
Blank Lines:       $($metrics.PowerShellCode.BlankLines)

CODE QUALITY METRICS
----------------------------------------
Classes:           $($metrics.CodeQuality.Classes)
Interfaces:        $($metrics.CodeQuality.Interfaces)
Methods:           $($metrics.CodeQuality.Methods)
Properties:        $($metrics.CodeQuality.Properties)
Total Complexity:  $($metrics.CodeQuality.CyclomaticComplexity)
Avg Complexity:    $($metrics.CodeQuality.AverageComplexity)
Max Complexity:    $($metrics.CodeQuality.MaxComplexity) (in $($metrics.CodeQuality.MaxComplexityLocation))

PROJECTS
----------------------------------------
"@
    
    foreach ($proj in $metrics.Projects) {
        $report += "`n$($proj.Name): $($proj.Files) files"
    }
    
    $report += "`n`n========================================`n"
    
    $report | Out-File -FilePath $MetricsOutputPath -Encoding UTF8
}

Write-Host "Metrics report saved to: $MetricsOutputPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  C# Files: $($metrics.Files.CSharp) ($($metrics.CSharpCode.CodeLines) lines of code)" -ForegroundColor White
Write-Host "  Projects: $($metrics.Files.Projects)" -ForegroundColor White
Write-Host "  Total C# Lines: $($metrics.CSharpCode.TotalLines)" -ForegroundColor White
Write-Host "  Classes: $($metrics.CodeQuality.Classes), Methods: $($metrics.CodeQuality.Methods)" -ForegroundColor White
Write-Host "  Average Complexity: $($metrics.CodeQuality.AverageComplexity)" -ForegroundColor White

#endregion

Write-Banner "All Steps Completed Successfully"
exit 0
