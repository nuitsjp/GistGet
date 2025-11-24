<#
.SYNOPSIS
    Collect static code metrics for the GistGet project.

.DESCRIPTION
    This script collects various static code metrics including:
    - Lines of code (total, source, comments, blank)
    - File counts by type
    - Project structure analysis
    - Code complexity metrics (if available)

.PARAMETER OutputPath
    Path to save the metrics report. Default is "metrics-report.txt".

.PARAMETER Format
    Output format: Text or Json. Default is Text.

.EXAMPLE
    .\Collect-Metrics.ps1
    .\Collect-Metrics.ps1 -OutputPath "reports/metrics.txt"
    .\Collect-Metrics.ps1 -Format Json -OutputPath "metrics.json"
#>

param(
    [string]$OutputPath = "metrics-report.txt",
    [string]$Format = "Text"
)

$ErrorActionPreference = "Stop"

# Get repository root path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Collecting GistGet Code Metrics" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to count lines in files
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

# Function to analyze code quality metrics
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
        # Count decision points: if, else, while, for, foreach, case, catch, &&, ||, ?
        $lines = Get-Content $file
        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            # Skip comments
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
            $complexity += ([regex]::Matches($trimmed, '\?')).Count - ([regex]::Matches($trimmed, '\?\?')).Count # Exclude null-coalescing
            
            $totalComplexity += $complexity
            
            # Track method with highest complexity (simplified - per line basis)
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

# Collect file statistics
Write-Host "Analyzing source files..." -ForegroundColor Green

$csFiles = Get-ChildItem -Path "$repoRoot\src" -Filter "*.cs" -Recurse -File
$csprojFiles = Get-ChildItem -Path "$repoRoot\src" -Filter "*.csproj" -Recurse -File
$yamlFiles = Get-ChildItem -Path "$repoRoot" -Filter "*.yaml" -Recurse -File
$ymlFiles = Get-ChildItem -Path "$repoRoot" -Filter "*.yml" -Recurse -File
$mdFiles = Get-ChildItem -Path "$repoRoot" -Filter "*.md" -Recurse -File
$ps1Files = Get-ChildItem -Path "$repoRoot" -Filter "*.ps1" -Recurse -File

# Count lines for C# files
$csMetrics = Get-LineCount -Files $csFiles.FullName

# Count lines for PowerShell files
$ps1Metrics = Get-LineCount -Files $ps1Files.FullName

# Analyze code quality metrics
Write-Host "Analyzing code quality metrics..." -ForegroundColor Green
$qualityMetrics = Get-CodeQualityMetrics -CsFiles $csFiles.FullName

# Build metrics object
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

# Analyze each project
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

# Resolve output path (make it absolute if relative)
if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path (Get-Location) $OutputPath
}

# Output results
Write-Host ""
Write-Host "Generating report..." -ForegroundColor Green

if ($Format -eq "Json") {
    $metrics | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Encoding UTF8
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
    
    $report | Out-File -FilePath $OutputPath -Encoding UTF8
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Metrics collection completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Report saved to: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Display summary
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  C# Files: $($metrics.Files.CSharp) ($($metrics.CSharpCode.CodeLines) lines of code)" -ForegroundColor White
Write-Host "  Projects: $($metrics.Files.Projects)" -ForegroundColor White
Write-Host "  Total C# Lines: $($metrics.CSharpCode.TotalLines)" -ForegroundColor White
Write-Host "  Classes: $($metrics.CodeQuality.Classes), Methods: $($metrics.CodeQuality.Methods)" -ForegroundColor White
Write-Host "  Average Complexity: $($metrics.CodeQuality.AverageComplexity)" -ForegroundColor White

exit 0
