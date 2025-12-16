<#
.SYNOPSIS
    ReSharper InspectCode を実行してコード品質の問題を検出します。

.DESCRIPTION
    ReSharper コマンドラインツールの InspectCode を使用して、ソリューション全体の
    コード品質問題を検出し、レポートを生成します。

.PARAMETER OutputPath
    レポートの出力先パス。既定値は inspectcode-report.sarif。

.PARAMETER OutputFormat
    出力フォーマット (sarif, xml, html, text)。既定値は sarif。

.PARAMETER Severity
    検出する最小の重大度レベル (HINT, SUGGESTION, WARNING, ERROR)。既定値は WARNING。

.PARAMETER Verbosity
    出力の詳細レベル (OFF, FATAL, ERROR, WARN, INFO, VERBOSE, TRACE)。既定値は WARN。

.PARAMETER NoBuild
    ビルドをスキップします。事前にビルド済みの場合に使用。

.EXAMPLE
    .\Run-ReSharperInspect.ps1
    既定の設定でコードインスペクションを実行

.EXAMPLE
    .\Run-ReSharperInspect.ps1 -Severity SUGGESTION -OutputFormat xml
    SUGGESTION 以上の問題を検出し、XML 形式で出力

.EXAMPLE
    .\Run-ReSharperInspect.ps1 -NoBuild
    ビルドをスキップしてインスペクションのみ実行

.NOTES
    前提条件: Setup-ReSharperCLT.ps1 を実行済みであること
#>

param(
    [string]$OutputPath = ".reports/inspectcode-report.sarif",
    [ValidateSet('sarif', 'xml', 'html', 'text')]
    [string]$OutputFormat = "sarif",
    [ValidateSet('HINT', 'SUGGESTION', 'WARNING', 'ERROR')]
    [string]$Severity = "WARNING",
    [ValidateSet('OFF', 'FATAL', 'ERROR', 'WARN', 'INFO', 'VERBOSE', 'TRACE')]
    [string]$Verbosity = "WARN",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$solutionPath = Join-Path $repoRoot "src\GistGet.slnx"

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

Write-Banner "ReSharper Code Inspection"

# ツールがインストールされているか確認
Push-Location $repoRoot
try {
    $toolList = dotnet tool list --local 2>&1
    if (-not ($toolList -match "jetbrains\.resharper\.globaltools")) {
        Write-Host "ReSharper CLI tools are not installed." -ForegroundColor Red
        Write-Host "Run .\scripts\Setup-ReSharperCLT.ps1 first." -ForegroundColor Yellow
        exit 1
    }
    
    # ツールを復元
    Write-Host "Restoring .NET tools..." -ForegroundColor Gray
    dotnet tool restore
    Test-LastExitCode "Failed to restore .NET tools"
}
finally {
    Pop-Location
}

# ビルド（オプション）
if (-not $NoBuild) {
    Write-Host ""
    Write-Host "Building solution..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        dotnet build $solutionPath -c Debug --verbosity quiet
        Test-LastExitCode "Build failed"
        Write-Host "Build completed." -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

# 出力パスを解決
if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

# Ensure output directory exists
$outputDir = Split-Path -Parent $OutputPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# InspectCode を実行
Write-Host ""
Write-Host "Running InspectCode..." -ForegroundColor Yellow
Write-Host "  Solution: $solutionPath" -ForegroundColor Gray
Write-Host "  Output: $OutputPath" -ForegroundColor Gray
Write-Host "  Format: $OutputFormat" -ForegroundColor Gray
Write-Host "  Severity: $Severity" -ForegroundColor Gray
Write-Host ""

Push-Location $repoRoot
try {
    $inspectArgs = @(
        "jb",
        "inspectcode",
        $solutionPath,
        "-o=$OutputPath",
        "--format=$OutputFormat",
        "--severity=$Severity",
        "--verbosity=$Verbosity"
    )
    
    # 既存の設定ファイルがあれば使用
    $dotsettingsPath = Join-Path $repoRoot "src\GistGet.slnx.DotSettings"
    if (Test-Path $dotsettingsPath) {
        $inspectArgs += "--profile=$dotsettingsPath"
        Write-Host "Using settings: $dotsettingsPath" -ForegroundColor Gray
    }
    
    & dotnet $inspectArgs
    $exitCode = $LASTEXITCODE
    
    if ($exitCode -ne 0) {
        Write-Host ""
        Write-Host "InspectCode finished with issues found (exit code: $exitCode)" -ForegroundColor Yellow
    }
    else {
        Write-Host ""
        Write-Host "InspectCode completed successfully." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

# レポートの概要を表示
if (Test-Path $OutputPath) {
    Write-Banner "Inspection Results"
    
    $fileInfo = Get-Item $OutputPath
    Write-Host "Report generated: $OutputPath" -ForegroundColor Green
    Write-Host "Report size: $([math]::Round($fileInfo.Length / 1024, 2)) KB" -ForegroundColor Gray
    
    # SARIF 形式の場合、問題数をカウント
    if ($OutputFormat -eq "sarif") {
        try {
            $sarif = Get-Content $OutputPath -Raw | ConvertFrom-Json
            $results = $sarif.runs[0].results
            
            if ($results) {
                $issueCount = $results.Count
                
                # 重大度別にグループ化
                $byLevel = $results | Group-Object { $_.level } | Sort-Object Name
                
                Write-Host ""
                Write-Host "Issues found: $issueCount" -ForegroundColor $(if ($issueCount -gt 0) { "Yellow" } else { "Green" })
                
                foreach ($group in $byLevel) {
                    $levelColor = switch ($group.Name) {
                        "error" { "Red" }
                        "warning" { "Yellow" }
                        "note" { "Cyan" }
                        default { "White" }
                    }
                    Write-Host "  $($group.Name): $($group.Count)" -ForegroundColor $levelColor
                }
                
                # ルール別のトップ5
                if ($issueCount -gt 0) {
                    Write-Host ""
                    Write-Host "Top issue types:" -ForegroundColor Yellow
                    $results | Group-Object { $_.ruleId } | 
                        Sort-Object Count -Descending | 
                        Select-Object -First 5 |
                        ForEach-Object {
                            Write-Host "  $($_.Name): $($_.Count)" -ForegroundColor Gray
                        }
                }
            }
            else {
                Write-Host "No issues found." -ForegroundColor Green
            }
        }
        catch {
            Write-Host "Could not parse SARIF report for summary." -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "Open the report file to view detailed results:" -ForegroundColor Cyan
    Write-Host "  $OutputPath" -ForegroundColor White
}

Write-Host ""
