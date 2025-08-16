# Windows Sandbox���ł�WinGet�Z�b�g�A�b�v�X�N���v�g
param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message, [string]$Color = "Green")
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $Color
}

function Write-Error-Status {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR: $Message" -ForegroundColor Red
}

try {
    Write-Status "Starting Windows Sandbox setup for GistGet testing..."
    
    # 1. WinGet�̃C���X�g�[���m�F
    Write-Status "Checking for existing WinGet installation..."
    
    $wingetPath = Get-Command winget -ErrorAction SilentlyContinue
    if ($wingetPath) {
        Write-Status "WinGet is already installed at: $($wingetPath.Source)"
        $wingetVersion = & winget --version
        Write-Status "WinGet version: $wingetVersion"
    } else {
        Write-Status "WinGet not found. Installing WinGet..." "Yellow"
        
        # Microsoft Store App Installer (WinGet)�̃_�E�����[�h�ƃC���X�g�[��
        $appInstallerUrl = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
        $downloadPath = "$env:TEMP\Microsoft.DesktopAppInstaller.msixbundle"
        
        Write-Status "Downloading App Installer from GitHub..."
        Invoke-WebRequest -Uri $appInstallerUrl -OutFile $downloadPath -UseBasicParsing
        
        Write-Status "Installing App Installer..."
        Add-AppxPackage -Path $downloadPath -ForceApplicationShutdown
        
        # �p�X�̍X�V
        $env:PATH = [Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [Environment]::GetEnvironmentVariable("PATH", "User")
        
        Write-Status "Verifying WinGet installation..."
        Start-Sleep -Seconds 5
        
        $wingetPath = Get-Command winget -ErrorAction SilentlyContinue
        if ($wingetPath) {
            $wingetVersion = & winget --version
            Write-Status "WinGet successfully installed! Version: $wingetVersion"
        } else {
            throw "WinGet installation failed or not found in PATH"
        }
    }
    
    # 2. WinGet�̊�{�ݒ�
    Write-Status "Configuring WinGet settings..."
    
    # WinGet�\�[�X�̍X�V
    Write-Status "Updating WinGet sources..."
    & winget source update --disable-interactivity
    
    # 3. .NET 8 Runtime�m�F�iGistGet����ɕK�v�j
    Write-Status "Checking .NET 8 Runtime..."
    
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetPath) {
        $dotnetVersion = & dotnet --version
        Write-Status ".NET is installed. Version: $dotnetVersion"
    } else {
        Write-Status ".NET not found. Installing .NET 8 Runtime..." "Yellow"
        
        try {
            & winget install Microsoft.DotNet.Runtime.8 --accept-source-agreements --accept-package-agreements --disable-interactivity
            Write-Status ".NET 8 Runtime installed successfully"
        } catch {
            Write-Status "Failed to install .NET 8 Runtime via WinGet. Trying direct download..." "Yellow"
            
            # ���ڃ_�E�����[�h�ƃC���X�g�[��
            $dotnetUrl = "https://download.microsoft.com/download/f/4/d/f4d9ceca-e38b-4ad4-a05e-b7a16b6dfdc6/windowsdesktop-runtime-8.0.8-win-x64.exe"
            $dotnetPath = "$env:TEMP\dotnet-runtime-8.0.8.exe"
            
            Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetPath -UseBasicParsing
            Start-Process -FilePath $dotnetPath -ArgumentList "/quiet" -Wait
            
            Write-Status ".NET 8 Runtime installed via direct download"
        }
    }
    
    # 4. GistGet�e�X�g���̏���
    Write-Status "Setting up GistGet test environment..."
    
    # GistGet.exe�̓���m�F
    if (Test-Path "C:\GistGet\GistGet.exe") {
        Write-Status "GistGet.exe found at C:\GistGet\GistGet.exe"
        
        try {
            $gistgetVersion = & "C:\GistGet\GistGet.exe" --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Status "GistGet version: $gistgetVersion"
            } else {
                # --version���Ȃ��ꍇ��help������
                $helpOutput = & "C:\GistGet\GistGet.exe" --help 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Status "GistGet.exe is working (help command successful)"
                } else {
                    Write-Status "GistGet.exe found but may have issues" "Yellow"
                }
            }
        } catch {
            Write-Status "Error testing GistGet.exe: $($_.Exception.Message)" "Yellow"
        }
    } else {
        Write-Error-Status "GistGet.exe not found at C:\GistGet\GistGet.exe"
    }
    
    # WinGet�}�j�t�F�X�g�̊m�F
    if (Test-Path "C:\Manifests\NuitsJp\GistGet\1.0.0") {
        Write-Status "WinGet manifests found at C:\Manifests\"
    } else {
        Write-Status "WinGet manifests not found at C:\Manifests\" "Yellow"
    }
    
    # 5. �e�X�g���̈ē�
    Write-Status "=" * 60 "Cyan"
    Write-Status "Windows Sandbox Test Environment Ready!" "Cyan"
    Write-Status "=" * 60 "Cyan"
    Write-Status ""
    Write-Status "Available test commands:" "Cyan"
    Write-Status "  Basic tests:"
    Write-Status "    cd C:\GistGet"
    Write-Status "    .\GistGet.exe --help"
    Write-Status "    .\GistGet.exe list"
    Write-Status "    .\GistGet.exe search git"
    Write-Status ""
    Write-Status "  Authentication tests:"
    Write-Status "    .\GistGet.exe login"
    Write-Status "    .\GistGet.exe gist status"
    Write-Status ""
    Write-Status "  WinGet manifest tests:"
    Write-Status "    winget install --manifest C:\Manifests\NuitsJp\GistGet\1.0.0\"
    Write-Status ""
    Write-Status "  Silent mode tests:"
    Write-Status "    .\GistGet.exe --silent install Git.Git"
    Write-Status ""
    Write-Status "Environment details:" "Green"
    Write-Status "  WinGet: $(if ($wingetPath) { $wingetVersion } else { 'Not installed' })"
    Write-Status "  .NET: $(if ($dotnetPath) { $dotnetVersion } else { 'Not installed' })"
    Write-Status "  GistGet: Available at C:\GistGet\GistGet.exe"
    Write-Status "  Manifests: Available at C:\Manifests\"
    Write-Status ""
    Write-Status "Ready for testing! ?" "Green"
    
} catch {
    Write-Error-Status "Setup failed: $($_.Exception.Message)"
    Write-Error-Status "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}