<#
.SYNOPSIS
    WinGet PR の自動実行を空プッシュで再トリガーします。

.DESCRIPTION
    microsoft/winget-pkgs へのPRで自動実行（CI/CD）がエラーになった場合、
    空コミットをプッシュすることでパイプラインを再実行させます。

    このスクリプトは external/winget-pkgs サブモジュールで動作し、
    指定されたブランチ（または現在のブランチ）に空コミットを作成してプッシュします。

.PARAMETER BranchName
    再実行するブランチ名。省略時は現在のブランチを使用します。

.PARAMETER Message
    空コミットのメッセージ。省略時はデフォルトメッセージを使用します。

.EXAMPLE
    .\scripts\Retry-WinGetPR.ps1
    現在のブランチで空プッシュを実行

.EXAMPLE
    .\scripts\Retry-WinGetPR.ps1 -BranchName NuitsJp.GistGet-1.2.0
    指定したブランチで空プッシュを実行

.EXAMPLE
    .\scripts\Retry-WinGetPR.ps1 -Message "Retry: Fix validation error"
    カスタムメッセージで空プッシュを実行
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$BranchName,

    [Parameter()]
    [string]$Message = "Empty commit to retrigger CI/CD pipeline"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

#region Constants and Paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
$wingetPkgsPath = Join-Path $repoRoot 'external/winget-pkgs'
#endregion

#region Helper Functions
function Write-Banner {
    param([string]$Title)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host $Message -ForegroundColor Green
}
#endregion

#region Validation
Write-Banner "WinGet PR Retry (Empty Push)"

# winget-pkgs サブモジュールの確認
if (-not (Test-Path $wingetPkgsPath)) {
    Write-Error @"
winget-pkgs サブモジュールが見つかりません。
以下のコマンドでサブモジュールを初期化してください:
    git submodule update --init external/winget-pkgs
"@
    exit 1
}
#endregion

#region Main Process
Push-Location $wingetPkgsPath
try {
    # ブランチ名が指定されていない場合は現在のブランチを取得
    if (-not $BranchName) {
        $BranchName = git rev-parse --abbrev-ref HEAD
        if ($LASTEXITCODE -ne 0) {
            Write-Error "現在のブランチ名の取得に失敗しました。"
            exit 1
        }
        Write-Host "現在のブランチ: $BranchName" -ForegroundColor Yellow
    } else {
        Write-Host "指定されたブランチ: $BranchName" -ForegroundColor Yellow

        # ブランチが存在するか確認
        $branchExists = git rev-parse --verify $BranchName 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Error "ブランチ '$BranchName' が見つかりません。"
            exit 1
        }

        # ブランチに切り替え
        Write-Step "ブランチ '$BranchName' に切り替え中..."
        git checkout $BranchName
        if ($LASTEXITCODE -ne 0) {
            Write-Error "ブランチの切り替えに失敗しました。"
            exit 1
        }
    }

    Write-Host "コミットメッセージ: $Message" -ForegroundColor Yellow
    Write-Host ""

    # 確認プロンプト
    $confirm = Read-Host "ブランチ '$BranchName' に空コミットをプッシュしてCI/CDを再実行しますか? (y/N)"
    if ($confirm -ne 'y') {
        Write-Host "処理を中止しました。" -ForegroundColor Red
        exit 0
    }

    # 空コミットを作成
    Write-Step "空コミットを作成中..."
    git commit --allow-empty -m $Message
    if ($LASTEXITCODE -ne 0) {
        Write-Error "空コミットの作成に失敗しました。"
        exit 1
    }

    # プッシュ
    Write-Step "ブランチ '$BranchName' をプッシュ中..."
    git push origin $BranchName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "プッシュに失敗しました。"
        exit 1
    }

    # 成功メッセージ
    Write-Banner "完了"
    Write-Host ""
    Write-Host "空コミットをプッシュしました。" -ForegroundColor Green
    Write-Host "GitHub でCI/CDパイプラインが再実行されます。" -ForegroundColor Green
    Write-Host ""
    Write-Host "PR確認: https://github.com/microsoft/winget-pkgs/pulls" -ForegroundColor Cyan
    Write-Host ""
}
finally {
    Pop-Location
}
#endregion
