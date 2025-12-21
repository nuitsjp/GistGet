<#
.SYNOPSIS
    GistGet プロジェクトのコード品質パイプラインを実行します。

.DESCRIPTION
    このスクリプトは以下の4ステップを順番に実行します:
    1. FormatCheck - コードフォーマットの検証
    2. Build - ソリューションのビルド（Roslyn 診断レポート込み）
    3. Tests - テスト実行（カバレッジ分析込み）
    4. ReSharper - ReSharper InspectCode（インストール済みの場合）

    ステップ制御パラメータを指定しない場合は全ステップを実行します。
    1つ以上指定した場合は、指定したステップのみを実行します。

.PARAMETER FormatCheck
    FormatCheck ステップを実行します。

.PARAMETER Build
    Build ステップを実行します。

.PARAMETER Tests
    Tests ステップを実行します。

.PARAMETER ReSharper
    ReSharper ステップを実行します。

.PARAMETER Configuration
    ビルド構成 (Debug または Release)。既定値は Debug。

.PARAMETER CoverageThreshold
    最低限必要なラインカバレッジ率 (%)。既定値は 98。0 を指定すると閾値チェックをスキップ。

.PARAMETER BranchCoverageThreshold
    最低限必要なブランチカバレッジ率 (%)。既定値は 85。0 を指定すると閾値チェックをスキップ。

.PARAMETER Top
    カバレッジが低いファイルの表示数。既定値は 5。

.PARAMETER TreatWarningsAsErrors
    ビルド警告をエラーとして扱います。

.PARAMETER ReSharperSeverity
    ReSharper InspectCode の検出する最小の重大度レベル (HINT, SUGGESTION, WARNING, ERROR)。既定値は SUGGESTION。

.PARAMETER ReSharperOutputPath
    ReSharper レポートの出力先パス。既定値は .reports/inspectcode-report.sarif。

.EXAMPLE
    .\Run-CodeQuality.ps1
    全ステップを実行（FormatCheck → Build → Tests → ReSharper）

.EXAMPLE
    .\Run-CodeQuality.ps1 -Build
    Build ステップのみ実行

.EXAMPLE
    .\Run-CodeQuality.ps1 -Build -Tests
    Build と Tests ステップのみ実行

.EXAMPLE
    .\Run-CodeQuality.ps1 -Tests -CoverageThreshold 95
    Tests ステップをカバレッジ閾値 95% で実行
#>

param(
    # ステップ制御パラメータ（未指定時は全ステップ実行、指定時は指定ステップのみ実行）
    [switch]$FormatCheck,
    [switch]$Build,
    [switch]$Tests,
    [switch]$ReSharper,
    
    # 設定値パラメータ
    [string]$Configuration = "Debug",
    [double]$CoverageThreshold = 98,
    [double]$BranchCoverageThreshold = 85,
    [int]$Top = 5,
    [switch]$TreatWarningsAsErrors,
    [ValidateSet('HINT', 'SUGGESTION', 'WARNING', 'ERROR')]
    [string]$ReSharperSeverity = "SUGGESTION",
    [string]$ReSharperOutputPath = ".reports/inspectcode-report.sarif",
    [string[]]$ExcludePatterns = @(
        '(^|[\\/])obj[\\/]',
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

# Force .NET/CLI UI culture to English to avoid culture initialization issues in JetBrains CLI tools
$env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "0"
$env:DOTNET_CLI_UI_LANGUAGE = "en"
$env:COMPlus_DefaultThreadCurrentCulture = "en-US"
$env:COMPlus_DefaultThreadCurrentUICulture = "en-US"

#region Common Utilities
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$runSettingsPath = Join-Path $repoRoot "coverlet.runsettings"
$platform = "x64"
$solutionPath = Join-Path $repoRoot "src\GistGet.slnx"
$testProjectPath = Join-Path $repoRoot "src\GistGet.Test\GistGet.Test.csproj"
$reportsDir = Join-Path $repoRoot ".reports"
$diagnosticsLogPath = Join-Path $reportsDir "build-diagnostics.log"

# サマリー用の結果を格納する変数（FormatCheck → Build → Tests → ReSharper の順）
$script:pipelineResults = @{
    FormatCheck = @{ Status = "Skipped"; Details = "" }
    Build = @{ Status = "Skipped"; Details = "" }
    Tests = @{ Status = "Skipped"; Details = "" }
    ReSharper = @{ Status = "Skipped"; Details = "" }
}

# 実行するステップを決定（未指定時は全ステップ実行）
$runAll = -not ($FormatCheck -or $Build -or $Tests -or $ReSharper)
$runFormatCheck = $runAll -or $FormatCheck
$runBuild = $runAll -or $Build
$runTests = $runAll -or $Tests
$runReSharper = $runAll -or $ReSharper

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

function Test-ReSharperInstalled {
    Push-Location $repoRoot
    try {
        $toolList = dotnet tool list --local 2>&1
        return ($toolList -match "jetbrains\.resharper\.globaltools")
    }
    finally {
        Pop-Location
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
    $totalBranches = 0
    $coveredBranches = 0
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
            # Calculate branch coverage from lines with branches
            if ($line.'branch' -eq 'True' -and $line.'condition-coverage') {
                # Parse condition-coverage format: "50% (1/2)"
                $condCoverage = $line.'condition-coverage'
                if ($condCoverage -match '\((\d+)/(\d+)\)') {
                    $totalBranches += [int]$Matches[2]
                    $coveredBranches += [int]$Matches[1]
                }
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
    $branchCoverage = if ($totalBranches -eq 0) { 100 } else { ($coveredBranches / $totalBranches) * 100 }

    return [pscustomobject]@{
        FilePath      = $FilePath
        LineCoverage  = $lineCoverage
        BranchCoverage = $branchCoverage
        Files         = $files
        CoveredLines  = $coveredTotal
        TotalLines    = $linesTotal
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
    $regexMatches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)

    foreach ($match in $regexMatches) {
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

#region ReSharper Functions
function Invoke-ReSharperInspectCode {
    param(
        [string]$OutputPath,
        [string]$Severity
    )

    # Resolve output path
    if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath = Join-Path $repoRoot $OutputPath
    }

    # Ensure output directory exists
    $outputDir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Write-Host "Running InspectCode..." -ForegroundColor Yellow
    Write-Host "  Solution: $solutionPath" -ForegroundColor Gray
    Write-Host "  Output: $OutputPath" -ForegroundColor Gray
    Write-Host "  Severity: $Severity" -ForegroundColor Gray
    Write-Host ""

    Push-Location $repoRoot
    try {
        # Restore tools first
        dotnet tool restore 2>&1 | Out-Null

        $inspectArgs = @(
            "jb",
            "inspectcode",
            $solutionPath,
            "-o=$OutputPath",
            "--format=sarif",
            "--severity=$Severity",
            "--no-buildin-settings",
            "--verbosity=WARN"
        )

        # 既存の設定ファイルがあれば使用
        $dotsettingsPath = Join-Path $repoRoot "src\GistGet.slnx.DotSettings"
        if (Test-Path $dotsettingsPath) {
            $inspectArgs += "--profile=$dotsettingsPath"
            Write-Host "Using settings: $dotsettingsPath" -ForegroundColor Gray
        }

        & dotnet $inspectArgs
        $exitCode = $LASTEXITCODE

        # Parse SARIF and return summary
        $summary = @{
            ExitCode = $exitCode
            OutputPath = $OutputPath
            IssueCount = 0
            ByLevel = @{}
            TopRules = @()
        }

        if (Test-Path $OutputPath) {
            try {
                $sarif = Get-Content $OutputPath -Raw | ConvertFrom-Json
                $results = $sarif.runs[0].results

                if ($results) {
                    # Filter out issues that are set to DO_NOT_SHOW in .DotSettings
                    # ReSharper CLI ignores DO_NOT_SHOW when using --severity parameter
                    $suppressedRules = @(
                        'SuggestVarOrType_BuiltInTypes',
                        'SuggestVarOrType_SimpleTypes',
                        'SuggestVarOrType_Elsewhere',
                        'MemberCanBePrivate.Global',
                        'PropertyCanBeMadeInitOnly.Global',
                        'ClassNeverInstantiated.Global',
                        'UnusedType.Global',
                        'VirtualMemberNeverOverridden.Global',
                        'EmptyNamespace',
                        'MemberCanBeProtected.Global',
                        'UnusedMethodReturnValue.Global'
                    )

                    $results = $results | Where-Object { $_.ruleId -notin $suppressedRules }

                    $summary.IssueCount = $results.Count

                    # Group by level
                    $results | Group-Object { $_.level } | ForEach-Object {
                        $summary.ByLevel[$_.Name] = $_.Count
                    }

                    # Top rules
                    $summary.TopRules = $results | Group-Object { $_.ruleId } |
                        Sort-Object Count -Descending |
                        Select-Object -First 5 |
                        ForEach-Object { @{ RuleId = $_.Name; Count = $_.Count } }
                }
            }
            catch {
                Write-Host "Could not parse SARIF report for summary." -ForegroundColor Gray
            }
        }

        return $summary
    }
    finally {
        Pop-Location
    }
}

function Write-ReSharperReport {
    param([hashtable]$Summary)

    if (-not $Summary) {
        Write-Host "No ReSharper data available." -ForegroundColor Yellow
        return
    }

    $fileInfo = Get-Item $Summary.OutputPath -ErrorAction SilentlyContinue
    if ($fileInfo) {
        Write-Host "Report generated: $($Summary.OutputPath)" -ForegroundColor Green
        Write-Host "Report size: $([math]::Round($fileInfo.Length / 1024, 2)) KB" -ForegroundColor Gray
    }

    Write-Host ""
    $issueColor = if ($Summary.IssueCount -gt 0) { "Yellow" } else { "Green" }
    Write-Host "Issues found: $($Summary.IssueCount)" -ForegroundColor $issueColor

    foreach ($level in $Summary.ByLevel.Keys | Sort-Object) {
        $levelColor = switch ($level) {
            "error" { "Red" }
            "warning" { "Yellow" }
            "note" { "Cyan" }
            default { "White" }
        }
        Write-Host "  ${level}: $($Summary.ByLevel[$level])" -ForegroundColor $levelColor
    }

    if ($Summary.TopRules.Count -gt 0) {
        Write-Host ""
        Write-Host "Top issue types:" -ForegroundColor Yellow
        foreach ($rule in $Summary.TopRules) {
            Write-Host "  $($rule.RuleId): $($rule.Count)" -ForegroundColor Gray
        }
    }
}
#endregion

#region Final Summary
function Write-PipelineSummary {
    Write-Banner "PIPELINE EXECUTION SUMMARY"

    $statusSymbols = @{
        "Passed" = @{ Symbol = "[OK]"; Color = "Green" }
        "Failed" = @{ Symbol = "[NG]"; Color = "Red" }
        "Warning" = @{ Symbol = "[!]"; Color = "Yellow" }
        "Skipped" = @{ Symbol = "[-]"; Color = "Gray" }
    }

    Write-Host ""
    Write-Host "STEP RESULTS" -ForegroundColor Yellow
    Write-Host "----------------------------------------"

    $steps = @(
        @{ Name = "Format Check"; Key = "FormatCheck" },
        @{ Name = "Build"; Key = "Build" },
        @{ Name = "Tests"; Key = "Tests" },
        @{ Name = "ReSharper InspectCode"; Key = "ReSharper" }
    )

    $hasFailure = $false

    foreach ($step in $steps) {
        $result = $script:pipelineResults[$step.Key]
        $statusInfo = $statusSymbols[$result.Status]

        $symbol = $statusInfo.Symbol
        $color = $statusInfo.Color

        $line = "{0,-5} {1,-25}" -f $symbol, $step.Name
        if ($result.Details) {
            $line += " - $($result.Details)"
        }

        Write-Host $line -ForegroundColor $color

        if ($result.Status -eq "Failed") {
            $hasFailure = $true
        }
    }

    Write-Host ""
    Write-Host "----------------------------------------"

    if ($hasFailure) {
        Write-Host "OVERALL: FAILED" -ForegroundColor Red
    } else {
        Write-Host "OVERALL: SUCCESS" -ForegroundColor Green
    }

    Write-Host ""
}
#endregion

#region Main Execution

Write-Banner "GistGet Code Quality Pipeline"
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Line Coverage Threshold: $CoverageThreshold%" -ForegroundColor Yellow
Write-Host "Branch Coverage Threshold: $BranchCoverageThreshold%" -ForegroundColor Yellow
Write-Host "Platform: $platform" -ForegroundColor Yellow

# 実行するステップを表示
if ($runAll) {
    Write-Host "Steps: All" -ForegroundColor Yellow
} else {
    $selectedSteps = @()
    if ($runFormatCheck) { $selectedSteps += "FormatCheck" }
    if ($runBuild) { $selectedSteps += "Build" }
    if ($runTests) { $selectedSteps += "Tests" }
    if ($runReSharper) { $selectedSteps += "ReSharper" }
    Write-Host "Steps: $($selectedSteps -join ', ')" -ForegroundColor Yellow
}

# Check ReSharper availability upfront (only if needed)
$resharperAvailable = $false
if ($runReSharper) {
    $resharperAvailable = Test-ReSharperInstalled
    if ($resharperAvailable) {
        Write-Host "ReSharper: Available" -ForegroundColor Green
    } else {
        Write-Host "ReSharper: Not installed (will be skipped)" -ForegroundColor Yellow
    }
}

# Ensure reports directory exists
if (-not (Test-Path $reportsDir)) {
    New-Item -ItemType Directory -Path $reportsDir -Force | Out-Null
}

# ============================================
# Step 1: Format Check
# ============================================
if ($runFormatCheck) {
    Write-Banner "Step: Format Check"

    Write-Host "Running dotnet format --verify-no-changes..." -ForegroundColor Gray
    $formatOutput = & dotnet format $solutionPath --verify-no-changes --verbosity quiet 2>&1
    $formatExitCode = $LASTEXITCODE

    if ($formatExitCode -eq 0) {
        Write-Host "FORMAT CHECK: PASSED" -ForegroundColor Green
        $script:pipelineResults.FormatCheck = @{ Status = "Passed"; Details = "" }
    } else {
        Write-Host "FORMAT CHECK: FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "The following files need formatting:" -ForegroundColor Yellow
        $formatOutput | ForEach-Object { Write-Host "  $_" }
        Write-Host ""
        Write-Host "Run 'dotnet format $solutionPath' to fix formatting issues." -ForegroundColor Cyan
        $script:pipelineResults.FormatCheck = @{ Status = "Failed"; Details = "Files need formatting" }
        Write-PipelineSummary
        exit 1
    }
}

# ============================================
# Step 2: Build (includes Roslyn Diagnostics)
# ============================================
if ($runBuild) {
    Write-Banner "Step: Build"

    $buildArgs = @(
        "build",
        $solutionPath,
        "-c", $Configuration,
        "-p:Platform=$platform"
    )

    if ($TreatWarningsAsErrors) {
        $buildArgs += "-p:TreatWarningsAsErrors=true"
    }

    # Capture diagnostics to a log file for analysis
    Write-Host "Building solution with diagnostics capture..." -ForegroundColor Gray
    $buildOutput = & dotnet $buildArgs 2>&1 | Tee-Object -FilePath $diagnosticsLogPath
    $buildExitCode = $LASTEXITCODE

    # Output build result
    $buildOutput | ForEach-Object { Write-Host $_ }

    if ($buildExitCode -ne 0) {
        Write-Host ""
        Write-Host "Build failed!" -ForegroundColor Red
        $script:pipelineResults.Build = @{ Status = "Failed"; Details = "Exit code: $buildExitCode" }
        Write-PipelineSummary
        exit $buildExitCode
    }

    # Roslyn Diagnostics Report (integrated into Build step)
    Write-Host ""
    Write-Host "ROSLYN DIAGNOSTICS" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    
    $diagnostics = Get-DiagnosticsSummary -LogPath $diagnosticsLogPath
    Write-DiagnosticsReport -Diagnostics $diagnostics -TopRules 10 -TopFiles 5

    # Extract and display CA1502 (cyclomatic complexity) violations
    if ($diagnostics -and $diagnostics.Warnings) {
        $complexityIssues = $diagnostics.Warnings | Where-Object { $_.Code -eq "CA1502" }
        if ($complexityIssues.Count -gt 0) {
            Write-Host ""
            Write-Host "HIGH COMPLEXITY METHODS (CA1502)" -ForegroundColor Yellow
            Write-Host "----------------------------------------"
            Write-Host "Methods exceeding cyclomatic complexity threshold (15):" -ForegroundColor Gray
            $complexityIssues | ForEach-Object {
                $fileName = Split-Path -Leaf $_.File
                Write-Host ("  {0}:{1} - {2}" -f $fileName, $_.Line, $_.Message) -ForegroundColor Yellow
            }
            Write-Host ""
            Write-Host "TIP: Consider refactoring methods with high complexity." -ForegroundColor Cyan
        }
    }

    # Determine Build status based on diagnostics
    $roslynDetails = "Configuration: $Configuration"
    if ($diagnostics) {
        $errorCount = $diagnostics.Errors.Count
        $warningCount = $diagnostics.Warnings.Count
        if ($errorCount -gt 0) {
            $script:pipelineResults.Build = @{ Status = "Failed"; Details = "$roslynDetails, $errorCount errors" }
            Write-PipelineSummary
            exit 1
        } elseif ($warningCount -gt 0) {
            $roslynDetails += ", $warningCount warnings"
        }
    }
    
    $script:pipelineResults.Build = @{ Status = "Passed"; Details = $roslynDetails }

    # Cleanup diagnostics log
    if (Test-Path $diagnosticsLogPath) {
        Remove-Item $diagnosticsLogPath -Force
    }
}

# ============================================
# Step 3: Tests (includes Coverage Analysis)
# ============================================
if ($runTests) {
    Write-Banner "Step: Tests"

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
    if ($LASTEXITCODE -ne 0) {
        $script:pipelineResults.Tests = @{ Status = "Failed"; Details = "Exit code: $LASTEXITCODE" }
        Write-PipelineSummary
        exit $LASTEXITCODE
    }

    # Coverage Analysis (integrated into Tests step)
    Write-Host ""
    Write-Host "COVERAGE ANALYSIS" -ForegroundColor Yellow
    Write-Host "----------------------------------------"

    $coverageFile = Get-LatestCoverageFile -Root "$repoRoot\TestResults"
    $summary = Get-CoverageSummary -FilePath $coverageFile.FullName -ExcludePatterns $ExcludePatterns

    Write-Host "Coverage file: $($coverageFile.FullName)" -ForegroundColor Gray
    Write-Host ("Line coverage   : {0:N2}% ({1}/{2} lines)" -f $summary.LineCoverage, $summary.CoveredLines, $summary.TotalLines) -ForegroundColor Cyan
    Write-Host ("Branch coverage : {0:N2}%" -f $summary.BranchCoverage) -ForegroundColor Cyan

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
        # Check individual file threshold (89%)
        $fileThreshold = 89
        $lowCoverageFiles = $summary.Files | Where-Object { $_.Coverage -lt $fileThreshold -and $_.Total -gt 0 }
        if ($lowCoverageFiles) {
            Write-Host ""
            Write-Host ("Files below {0}% coverage:" -f $fileThreshold) -ForegroundColor Red
            $lowCoverageFiles | Sort-Object Coverage | ForEach-Object {
                Write-Host ("  {0,-70} {1,6:N2}% ({2}/{3})" -f $_.File, $_.Coverage, $_.Covered, $_.Total) -ForegroundColor Red
            }
            $script:pipelineResults.Tests = @{ Status = "Failed"; Details = "{0} files below {1}%" -f $lowCoverageFiles.Count, $fileThreshold }
            Write-PipelineSummary
            exit 1
        }

        # Check overall threshold (98%)
        if ($summary.LineCoverage -lt $CoverageThreshold) {
            Write-Host ("Coverage threshold not met: {0:N2}% < {1:N2}%" -f $summary.LineCoverage, $CoverageThreshold) -ForegroundColor Red
            $script:pipelineResults.Tests = @{ Status = "Failed"; Details = "Line: {0:N2}% < {1:N2}%" -f $summary.LineCoverage, $CoverageThreshold }
            Write-PipelineSummary
            exit 1
        }

        Write-Host ("Line coverage threshold met: {0:N2}% >= {1:N2}%" -f $summary.LineCoverage, $CoverageThreshold) -ForegroundColor Green
    }

    # Check branch coverage threshold
    if ($BranchCoverageThreshold -gt 0) {
        if ($summary.BranchCoverage -lt $BranchCoverageThreshold) {
            Write-Host ("Branch coverage threshold not met: {0:N2}% < {1:N2}%" -f $summary.BranchCoverage, $BranchCoverageThreshold) -ForegroundColor Red
            $script:pipelineResults.Tests = @{ Status = "Failed"; Details = "Branch: {0:N2}% < {1:N2}%" -f $summary.BranchCoverage, $BranchCoverageThreshold }
            Write-PipelineSummary
            exit 1
        }
        Write-Host ("Branch coverage threshold met: {0:N2}% >= {1:N2}%" -f $summary.BranchCoverage, $BranchCoverageThreshold) -ForegroundColor Green
    }

    $script:pipelineResults.Tests = @{ Status = "Passed"; Details = "Line: {0:N2}%, Branch: {1:N2}%" -f $summary.LineCoverage, $summary.BranchCoverage }
}

# ============================================
# Step 4: ReSharper InspectCode
# ============================================
if ($runReSharper) {
    Write-Banner "Step: ReSharper InspectCode"

    if ($resharperAvailable) {
        $resharperSummary = Invoke-ReSharperInspectCode -OutputPath $ReSharperOutputPath -Severity $ReSharperSeverity
        Write-ReSharperReport -Summary $resharperSummary

        if ($resharperSummary.IssueCount -gt 0) {
            $errorCount = if ($resharperSummary.ByLevel.ContainsKey("error")) { $resharperSummary.ByLevel["error"] } else { 0 }
            $warningCount = if ($resharperSummary.ByLevel.ContainsKey("warning")) { $resharperSummary.ByLevel["warning"] } else { 0 }

            if ($errorCount -gt 0) {
                $script:pipelineResults.ReSharper = @{ Status = "Warning"; Details = "$($resharperSummary.IssueCount) issues ($errorCount errors)" }
            } else {
                $script:pipelineResults.ReSharper = @{ Status = "Warning"; Details = "$($resharperSummary.IssueCount) issues" }
            }
        } else {
            $script:pipelineResults.ReSharper = @{ Status = "Passed"; Details = "No issues" }
        }
    } else {
        Write-Host "ReSharper CLI tools are not installed. Skipping..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To install ReSharper CLI tools, run:" -ForegroundColor Cyan
        Write-Host "  .\scripts\Setup-ReSharperCLT.ps1" -ForegroundColor White
        $script:pipelineResults.ReSharper = @{ Status = "Skipped"; Details = "Not installed" }
    }
}

#endregion

# エンコーディングを元に戻す
[Console]::OutputEncoding = $originalOutputEncoding
$OutputEncoding = $originalPSOutputEncoding

# Final Summary
Write-PipelineSummary
exit 0
