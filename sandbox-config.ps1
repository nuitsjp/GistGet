# Windows Sandbox設定ファイル生成スクリプト
# 現在の環境に合わせてsandbox.wsbを生成します

param(
    [string]$ProjectRoot = $PSScriptRoot,
    [string]$OutputFile = "sandbox.wsb"
)

$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

try {
    Write-Info "Generating Windows Sandbox configuration..."
    
    # パスの正規化
    $projectRoot = Resolve-Path $ProjectRoot
    $publishPath = Join-Path $projectRoot "src\NuitsJp.GistGet\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish"
    $manifestsPath = Join-Path $projectRoot "winget-manifests"
    $scriptsPath = Join-Path $projectRoot "scripts"
    
    # パスの存在確認
    if (-not (Test-Path $publishPath)) {
        Write-Warning "Publish path not found: $publishPath"
        Write-Warning "Run 'dotnet publish' first to generate the executable."
    }
    
    if (-not (Test-Path $manifestsPath)) {
        Write-Warning "Manifests path not found: $manifestsPath"
    }
    
    if (-not (Test-Path $scriptsPath)) {
        throw "Scripts path not found: $scriptsPath"
    }
    
    # WSB設定内容を生成
    $wsbContent = @"
<Configuration>
  <VGpu>Enable</VGpu>
  <Networking>Enable</Networking>
  <MappedFolders>
    <MappedFolder>
      <HostFolder>$publishPath</HostFolder>
      <SandboxFolder>C:\GistGet</SandboxFolder>
      <ReadOnly>true</ReadOnly>
    </MappedFolder>
    <MappedFolder>
      <HostFolder>$manifestsPath</HostFolder>
      <SandboxFolder>C:\Manifests</SandboxFolder>
      <ReadOnly>true</ReadOnly>
    </MappedFolder>
    <MappedFolder>
      <HostFolder>$scriptsPath</HostFolder>
      <SandboxFolder>C:\Scripts</SandboxFolder>
      <ReadOnly>true</ReadOnly>
    </MappedFolder>
  </MappedFolders>
  <LogonCommand>
    <Command>powershell.exe -ExecutionPolicy Bypass -File C:\Scripts\setup-sandbox.ps1</Command>
  </LogonCommand>
</Configuration>
"@

    # ファイル出力
    $outputPath = Join-Path $projectRoot $OutputFile
    $wsbContent | Out-File -FilePath $outputPath -Encoding UTF8
    
    Write-Info "Generated sandbox configuration: $outputPath"
    Write-Info "Mapped folders:"
    Write-Info "  GistGet.exe: $publishPath -> C:\GistGet"
    Write-Info "  Manifests: $manifestsPath -> C:\Manifests"
    Write-Info "  Scripts: $scriptsPath -> C:\Scripts"
    Write-Info ""
    Write-Info "To start testing, run: .\\$OutputFile"
    
} catch {
    Write-Error "Failed to generate sandbox configuration: $($_.Exception.Message)"
    exit 1
}