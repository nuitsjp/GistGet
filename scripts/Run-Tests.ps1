<#
.SYNOPSIS
    GistGet プロジェクトのテスト、カバレッジ分析、静的解析、メトリクス収集を実行します。

.DESCRIPTION
    このスクリプトは以下を順番に実行します:
    1. ソリューションのビルド（Roslyn アナライザー診断収集込み）
    2. テスト実行（カバレッジ収集込み）
    3. カバレッジ分析と閾値チェック
    4. コードメトリクス収集とレポート生成
    5. [Full モード] フォーマットチェック
    6. [Full モード] Roslyn 診断レポートとリファクタリング候補

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

.PARAMETER AnalysisMode
    解析モード。Quick はビルド・テスト・カバレッジのみ。Full (既定) は静的解析を含む。

.PARAMETER SkipTests
    テスト実行をスキップします。静的解析のみ実行したい場合に使用。

.PARAMETER TreatWarningsAsErrors
    ビルド警告をエラーとして扱います。

.EXAMPLE
    .\Run-Tests.ps1
    既定の Full モードでテスト、カバレッジ分析、静的解析を実行

.EXAMPLE
    .\Run-Tests.ps1 -AnalysisMode Quick
    Quick モードでビルド、テスト、カバレッジのみ実行（高速）

.EXAMPLE
    .\Run-Tests.ps1 -SkipTests
    テストをスキップしてビルドと静的解析のみ実行
#>

param(
    [string]$Configuration = "Debug",
    [double]$CoverageThreshold = 95,
    [int]$Top = 5,
    [string]$MetricsOutputPath = ".reports/metrics-report.txt",
    [ValidateSet('Text', 'Json')]
    [string]$MetricsFormat = "Text",
    [ValidateSet('Quick', 'Full')]
    [string]$AnalysisMode = "Full",
    [switch]$SkipTests,
    [switch]$TreatWarningsAsErrors,
    [string[]]$ExcludePatterns = @(
        '[\\/]obj[\\/]',
        '\.g\.cs$',
        'Program\.cs$',
        'GitHubClientFactory\.cs$'
    )
)

$ErrorActionPreference = "Stop"

# コンソールのエンコーディングをUTF-8に設定（文字化け防止）
$originalOutputEncoding = [Console]::OutputEncoding
$originalPSOutputEncoding = $OutputEncoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

#region Common Utilities
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$runSettingsPath = Join-Path $repoRoot "coverlet.runsettings"
$platform = "x64"
$solutionPath = Join-Path $repoRoot "src\GistGet.slnx"
$testProjectPath = Join-Path $repoRoot "src\GistGet.Test\GistGet.Test.csproj"
$reportsDir = Join-Path $repoRoot ".reports"
$diagnosticsLogPath = Join-Path $reportsDir "build-diagnostics.log"


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

#region Static Analysis Functions
function Get-DiagnosticsSummary {
    param([string]$LogPath)
    
    if (-not (Test-Path $LogPath)) {
        return $null
    }
    
    $content = Get-Content $LogPath -Raw
    $diagnostics = @{
        Errors = @()
        Warnings = @()
        Info = @()
        ByRule = @{}
        ByFile = @{}
    }
    
    # Parse MSBuild diagnostic output
    # Format: path(line,col): severity code: message
    $pattern = '([^(]+)\((\d+),(\d+)\):\s*(error|warning|info)\s+(\w+):\s*(.+)'
    $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    
    foreach ($match in $matches) {
        $file = $match.Groups[1].Value.Trim()
        $line = $match.Groups[2].Value
        $col = $match.Groups[3].Value
        $severity = $match.Groups[4].Value
        $code = $match.Groups[5].Value
        $message = $match.Groups[6].Value.Trim()
        
        $diagnostic = [pscustomobject]@{
            File = $file
            Line = [int]$line
            Column = [int]$col
            Severity = $severity
            Code = $code
            Message = $message
        }
        
        switch ($severity) {
            "error" { $diagnostics.Errors += $diagnostic }
            "warning" { $diagnostics.Warnings += $diagnostic }
            "info" { $diagnostics.Info += $diagnostic }
        }
        
        # Group by rule
        if (-not $diagnostics.ByRule.ContainsKey($code)) {
            $diagnostics.ByRule[$code] = @{
                Code = $code
                Message = $message
                Count = 0
                Severity = $severity
            }
        }
        $diagnostics.ByRule[$code].Count++
        
        # Group by file
        $fileName = Split-Path -Leaf $file
        if (-not $diagnostics.ByFile.ContainsKey($fileName)) {
            $diagnostics.ByFile[$fileName] = 0
        }
        $diagnostics.ByFile[$fileName]++
    }
    
    return $diagnostics
}

function Write-DiagnosticsReport {
    param(
        [hashtable]$Diagnostics,
        [int]$TopRules = 10,
        [int]$TopFiles = 5
    )
    
    if (-not $Diagnostics) {
        Write-Host "No diagnostics data available." -ForegroundColor Yellow
        return
    }
    
    $totalErrors = $Diagnostics.Errors.Count
    $totalWarnings = $Diagnostics.Warnings.Count
    $totalInfo = $Diagnostics.Info.Count
    
    Write-Host ""
    Write-Host "DIAGNOSTICS SUMMARY" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    
    if ($totalErrors -gt 0) {
        Write-Host ("Errors:     {0}" -f $totalErrors) -ForegroundColor Red
    } else {
        Write-Host ("Errors:     {0}" -f $totalErrors) -ForegroundColor Green
    }
    
    if ($totalWarnings -gt 0) {
        Write-Host ("Warnings:   {0}" -f $totalWarnings) -ForegroundColor Yellow
    } else {
        Write-Host ("Warnings:   {0}" -f $totalWarnings) -ForegroundColor Green
    }
    
    Write-Host ("Info:       {0}" -f $totalInfo) -ForegroundColor Gray
    
    # Top refactoring opportunities
    if ($Diagnostics.ByRule.Count -gt 0) {
        Write-Host ""
        Write-Host "TOP REFACTORING OPPORTUNITIES" -ForegroundColor Yellow
        Write-Host "----------------------------------------"
        
        $Diagnostics.ByRule.Values | 
            Sort-Object Count -Descending | 
            Select-Object -First $TopRules |
            ForEach-Object {
                $sevColor = switch ($_.Severity) {
                    "error" { "Red" }
                    "warning" { "Yellow" }
                    default { "Gray" }
                }
                
                # Truncate message to fit
                $msg = $_.Message
                if ($msg.Length -gt 45) {
                    $msg = $msg.Substring(0, 42) + "..."
                }
                
                Write-Host ("  {0,-8} {1,-45} ({2} occurrences)" -f $_.Code, $msg, $_.Count) -ForegroundColor $sevColor
            }
    }
    
    # Diagnostics by file
    if ($Diagnostics.ByFile.Count -gt 0) {
        Write-Host ""
        Write-Host "DIAGNOSTICS BY FILE (Top $TopFiles)" -ForegroundColor Yellow
        Write-Host "----------------------------------------"
        
        $Diagnostics.ByFile.GetEnumerator() |
            Sort-Object Value -Descending |
            Select-Object -First $TopFiles |
            ForEach-Object {
                Write-Host ("  {0,-50} {1} diagnostics" -f $_.Key, $_.Value)
            }
    }
}
#endregion

#region Main Execution

Write-Banner "GistGet Test Pipeline"
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Analysis Mode: $AnalysisMode" -ForegroundColor Yellow
Write-Host "Coverage Threshold: $CoverageThreshold%" -ForegroundColor Yellow
Write-Host "Platform: $platform" -ForegroundColor Yellow
if ($SkipTests) {
    Write-Host "Tests: SKIPPED" -ForegroundColor Yellow
}

$stepNumber = 1

# Step 1: Build
Write-Banner "Step $stepNumber`: Building Solution"
$stepNumber++

$buildArgs = @(
    "build",
    $solutionPath,
    "-c", $Configuration,
    "-p:Platform=$platform"
)

if ($TreatWarningsAsErrors) {
    $buildArgs += "-p:TreatWarningsAsErrors=true"
}

if ($AnalysisMode -eq "Full") {
    # Capture diagnostics to a log file for later analysis
    Write-Host "Capturing diagnostics for analysis..." -ForegroundColor Gray
    $buildOutput = & dotnet $buildArgs 2>&1 | Tee-Object -FilePath $diagnosticsLogPath
    $buildExitCode = $LASTEXITCODE
    
    # Output build result
    $buildOutput | ForEach-Object { Write-Host $_ }
    
    if ($buildExitCode -ne 0) {
        Write-Host ""
        Write-Host "Build failed!" -ForegroundColor Red
        exit $buildExitCode
    }
} else {
    & dotnet $buildArgs
    Test-LastExitCode "Build failed!"
}

# Step 2: Test (unless skipped)
if (-not $SkipTests) {
    Write-Banner "Step $stepNumber`: Running Tests"
    $stepNumber++
    
    $testArgs = @(
        "test",
        $testProjectPath,
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
    Write-Banner "Step $stepNumber`: Analyzing Coverage"
    $stepNumber++
    
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
}

# Step 4: Metrics Collection
Write-Banner "Step $stepNumber`: Collecting Metrics"
$stepNumber++

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
    AnalysisMode = $AnalysisMode
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
    $MetricsOutputPath = Join-Path $repoRoot $MetricsOutputPath
}

# Ensure reports directory exists
$metricsDir = Split-Path -Parent $MetricsOutputPath
if (-not (Test-Path $metricsDir)) {
    New-Item -ItemType Directory -Path $metricsDir -Force | Out-Null
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
Analysis Mode: $($metrics.AnalysisMode)

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

# Full Mode Only: Format Check and Diagnostics Report
if ($AnalysisMode -eq "Full") {
    # Step 5: Format Check
    Write-Banner "Step $stepNumber`: Format Check"
    $stepNumber++
    
    Write-Host "Running dotnet format --verify-no-changes..." -ForegroundColor Gray
    $formatOutput = & dotnet format $solutionPath --verify-no-changes --verbosity quiet 2>&1
    $formatExitCode = $LASTEXITCODE
    
    if ($formatExitCode -eq 0) {
        Write-Host "FORMAT CHECK: PASSED" -ForegroundColor Green
    } else {
        Write-Host "FORMAT CHECK: FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "The following files need formatting:" -ForegroundColor Yellow
        $formatOutput | ForEach-Object { Write-Host "  $_" }
        Write-Host ""
        Write-Host "Run 'dotnet format $solutionPath' to fix formatting issues." -ForegroundColor Cyan
        # Don't exit - continue to show diagnostics
    }
    
    # Step 6: Static Analysis Report
    Write-Banner "Step $stepNumber`: Static Analysis Report"
    $stepNumber++
    
    $diagnostics = Get-DiagnosticsSummary -LogPath $diagnosticsLogPath
    Write-DiagnosticsReport -Diagnostics $diagnostics -TopRules 10 -TopFiles 5
    
    # Cleanup diagnostics log
    if (Test-Path $diagnosticsLogPath) {
        Remove-Item $diagnosticsLogPath -Force
    }
}

#endregion

# エンコーディングを元に戻す
[Console]::OutputEncoding = $originalOutputEncoding
$OutputEncoding = $originalPSOutputEncoding

Write-Banner "All Steps Completed Successfully"
exit 0
