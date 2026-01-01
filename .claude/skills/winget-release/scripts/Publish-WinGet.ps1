#requires -Version 7.0
<#
.SYNOPSIS
    GistGet の完全なリリースパイプラインを実行します。

.DESCRIPTION
    このスクリプトは release.yml ワークフローの完全な置き換えとして、以下の処理を行います:
    1. Run-CodeQuality.ps1 を Release モードで実行（品質チェック）
    2. リリースビルドを作成し ZIP アーカイブを生成
    3. Git タグを作成
    4. gh CLI で GitHub Release を作成
    5. winget-pkgs サブモジュールを upstream と同期
    6. WinGet マニフェストを生成してブランチを作成
    7. gh CLI で microsoft/winget-pkgs へ PR を作成

.PARAMETER Version
    リリースバージョン (例: 1.0.0)。指定しない場合は csproj から取得。

.PARAMETER SkipQualityCheck
    品質チェック（Run-CodeQuality.ps1）をスキップする場合に指定。

.PARAMETER SkipGitHubRelease
    GitHub Release の作成をスキップする場合に指定。

.PARAMETER SkipWinGetPR
    WinGet マニフェスト作成と PR の作成をスキップする場合に指定。

.PARAMETER SkipPRCreation
    WinGet マニフェストは作成するが、PR の作成だけをスキップする場合に指定。

.PARAMETER DryRun
    実際の変更を行わず、プレビューのみ行う場合に指定。

.PARAMETER Force
    確認プロンプトをスキップして実行する場合に指定。

.EXAMPLE
    .\skills\winget-release\scripts\Publish-WinGet.ps1
    csproj のバージョンで完全なリリースパイプラインを実行

.EXAMPLE
    .\skills\winget-release\scripts\Publish-WinGet.ps1 -Version 1.0.4
    バージョン 1.0.4 でリリースを実行

.EXAMPLE
    .\skills\winget-release\scripts\Publish-WinGet.ps1 -DryRun
    プレビューのみ（実際のリリースは行わない）

.EXAMPLE
    .\skills\winget-release\scripts\Publish-WinGet.ps1 -SkipQualityCheck -SkipGitHubRelease
    品質チェックと GitHub Release をスキップして WinGet PR のみ作成
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version,

    [Parameter()]
    [switch]$SkipQualityCheck,

    [Parameter()]
    [switch]$SkipGitHubRelease,

    [Parameter()]
    [switch]$SkipWinGetPR,

    [Parameter()]
    [switch]$SkipPRCreation,

    [Parameter()]
    [switch]$DryRun,

    [Parameter()]
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

#region Constants and Paths
$PackageIdentifier = 'NuitsJp.GistGet'
$Publisher = 'nuitsjp'
$PackageName = 'GistGet'
$GitHubOwner = 'nuitsjp'
$GitHubRepo = 'GistGet'
$WinGetUpstreamOwner = 'microsoft'
$WinGetUpstreamRepo = 'winget-pkgs'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
$projectPath = Join-Path $repoRoot 'src/GistGet/GistGet.csproj'
$wingetPkgsPath = Join-Path $repoRoot 'external/winget-pkgs'
$artifactsPath = Join-Path $repoRoot 'artifacts'
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
    param([string]$Step, [string]$Description)
    Write-Host ""
    Write-Host "[$Step] $Description" -ForegroundColor Green
}

function Test-Command {
    param([string]$Command)
    return $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

function Get-VersionFromCsproj {
    [xml]$csproj = Get-Content $projectPath
    $ver = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $ver) {
        throw "バージョンが csproj に定義されていません。-Version パラメータで指定してください。"
    }
    return $ver
}
#endregion

#region Validation
Write-Banner "GistGet Release Pipeline"

# gh CLI の確認
if (-not (Test-Command 'gh')) {
    Write-Error @"
gh CLI がインストールされていません。
インストール方法: winget install GitHub.cli
"@
    exit 1
}

# gh CLI の認証確認
$ghAuthStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error @"
gh CLI が認証されていません。
以下のコマンドで認証してください:
    gh auth login
"@
    exit 1
}

# winget-pkgs サブモジュールの確認
if (-not (Test-Path $wingetPkgsPath)) {
    Write-Error @"
winget-pkgs サブモジュールが見つかりません。
以下のコマンドでサブモジュールを初期化してください:
    git submodule update --init external/winget-pkgs
"@
    exit 1
}

# バージョンの取得
if (-not $Version) {
    $Version = Get-VersionFromCsproj
}

$tagName = "v$Version"
$zipName = "GistGet-win-x64.zip"
$branchName = "$PackageIdentifier-$Version"

Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Tag: $tagName" -ForegroundColor Yellow
Write-Host "DryRun: $DryRun" -ForegroundColor Yellow
Write-Host ""

# 確認プロンプト
if (-not $Force -and -not $DryRun) {
    $confirm = Read-Host "バージョン $Version でリリースを実行しますか? (y/N)"
    if ($confirm -ne 'y') {
        Write-Host "処理を中止しました。" -ForegroundColor Red
        exit 0
    }
}
#endregion

#region Step 1: Quality Check
if (-not $SkipQualityCheck) {
    Write-Banner "Step 1: Quality Check"

    $qualityScript = Join-Path $repoRoot 'scripts/Run-CodeQuality.ps1'
    if (-not (Test-Path $qualityScript)) {
        Write-Error "Run-CodeQuality.ps1 が見つかりません: $qualityScript"
        exit 1
    }

    Write-Host "Run-CodeQuality.ps1 を Release モードで実行中..." -ForegroundColor Yellow
    & $qualityScript -Configuration Release

    if ($LASTEXITCODE -ne 0) {
        Write-Error "品質チェックに失敗しました。リリースを中止します。"
        exit $LASTEXITCODE
    }

    Write-Host "品質チェック: OK" -ForegroundColor Green
} else {
    Write-Host "Step 1: Quality Check - スキップ" -ForegroundColor Yellow
}
#endregion

#region Step 2: Build Release
Write-Banner "Step 2: Build Release"

# 出力ディレクトリの準備
if (Test-Path $artifactsPath) {
    Remove-Item -Recurse -Force $artifactsPath
}
New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

$publishPath = Join-Path $artifactsPath "publish/win-x64"
$zipPath = Join-Path $artifactsPath $zipName

# dotnet publish
Write-Step "2.1" "dotnet publish 実行中..."
$publishArgs = @(
    'publish'
    $projectPath
    '-c', 'Release'
    '-r', 'win-x64'
    '-o', $publishPath
    "-p:Version=$Version"
    '--self-contained', 'true'
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "ビルドに失敗しました。"
    exit $LASTEXITCODE
}

# ZIP 作成
Write-Step "2.2" "ZIP アーカイブを作成中..."
Compress-Archive -Path "$publishPath/*" -DestinationPath $zipPath -Force

# SHA256 計算
$sha256 = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
Write-Host "SHA256: $sha256" -ForegroundColor Cyan

# SHA256SUMS.txt 作成
$hashFilePath = Join-Path $artifactsPath "SHA256SUMS.txt"
"$sha256  $zipName" | Set-Content -Path $hashFilePath -Encoding UTF8

Write-Host "ビルド完了: $zipPath" -ForegroundColor Green
#endregion

#region Step 3: Create Git Tag
Write-Banner "Step 3: Create Git Tag"

Push-Location $repoRoot
try {
    # 既存タグの確認
    $existingTag = git tag -l $tagName 2>$null
    if ($existingTag) {
        Write-Warning "タグ $tagName は既に存在します。"
        if (-not $Force -and -not $DryRun) {
            $confirm = Read-Host "既存のタグを削除して再作成しますか? (y/N)"
            if ($confirm -ne 'y') {
                Write-Host "タグの作成をスキップします。" -ForegroundColor Yellow
            } else {
                if (-not $DryRun) {
                    git tag -d $tagName
                    git push origin --delete $tagName 2>$null
                }
            }
        }
    }

    if (-not $DryRun) {
        Write-Host "タグを作成: $tagName" -ForegroundColor Yellow
        git tag -a $tagName -m "Release $tagName"
        git push origin $tagName
        Write-Host "タグをプッシュしました: $tagName" -ForegroundColor Green
    } else {
        Write-Host "[DryRun] タグを作成: $tagName" -ForegroundColor Gray
    }
}
finally {
    Pop-Location
}
#endregion

#region Step 4: Create GitHub Release
if (-not $SkipGitHubRelease) {
    Write-Banner "Step 4: Create GitHub Release"

    $releaseNotes = @"
## GistGet v$Version

Windows Package Manager Cloud Sync Tool

### インストール方法

#### WinGet (推奨)
``````powershell
winget install NuitsJp.GistGet
``````

#### 手動インストール
1. お使いのアーキテクチャに対応した ZIP ファイルをダウンロード
2. 任意のフォルダに展開
3. 展開したフォルダを PATH 環境変数に追加

### ダウンロード
| アーキテクチャ | ファイル | SHA256 |
|---------------|----------|--------|
| x64 | $zipName | ``$sha256`` |

### 変更点
詳細は [CHANGELOG](https://github.com/$GitHubOwner/$GitHubRepo/blob/main/CHANGELOG.md) を参照してください。
"@

    if (-not $DryRun) {
        Write-Host "GitHub Release を作成中..." -ForegroundColor Yellow

        # 既存リリースの確認と削除
        $existingRelease = gh release view $tagName --repo "$GitHubOwner/$GitHubRepo" 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Warning "リリース $tagName は既に存在します。削除して再作成します。"
            gh release delete $tagName --repo "$GitHubOwner/$GitHubRepo" --yes
        }

        # リリースノートをUTF-8 BOMなしで書き出し（文字化け防止）
        $notesFile = Join-Path $artifactsPath "release-notes.md"
        [System.IO.File]::WriteAllText($notesFile, $releaseNotes, [System.Text.UTF8Encoding]::new($false))

        gh release create $tagName `
            --repo "$GitHubOwner/$GitHubRepo" `
            --title "GistGet $tagName" `
            --notes-file $notesFile `
            $zipPath `
            $hashFilePath

        # 一時ファイル削除
        Remove-Item $notesFile -ErrorAction SilentlyContinue

        if ($LASTEXITCODE -ne 0) {
            Write-Error "GitHub Release の作成に失敗しました。"
            exit $LASTEXITCODE
        }

        Write-Host "GitHub Release を作成しました: https://github.com/$GitHubOwner/$GitHubRepo/releases/tag/$tagName" -ForegroundColor Green
    } else {
        Write-Host "[DryRun] GitHub Release を作成: $tagName" -ForegroundColor Gray
        Write-Host "[DryRun] アップロードファイル: $zipPath, $hashFilePath" -ForegroundColor Gray
    }
} else {
    Write-Host "Step 4: Create GitHub Release - スキップ" -ForegroundColor Yellow
}
#endregion

#region Step 5: Sync winget-pkgs with upstream
if (-not $SkipWinGetPR) {
    Write-Banner "Step 5: Sync winget-pkgs"

    Push-Location $wingetPkgsPath
    try {
        # upstream リモートの追加（存在しない場合）
        $remotes = git remote
        if ($remotes -notcontains 'upstream') {
            Write-Host "upstream リモートを追加中..." -ForegroundColor Yellow
            git remote add upstream "https://github.com/$WinGetUpstreamOwner/$WinGetUpstreamRepo.git"
        }

        if (-not $DryRun) {
            # upstream から fetch
            Write-Host "upstream から fetch 中..." -ForegroundColor Yellow
            git fetch upstream master

            # master ブランチに切り替えて upstream を取り込み
            Write-Host "master ブランチを upstream/master にリセット中..." -ForegroundColor Yellow
            git checkout master
            git reset --hard upstream/master
        } else {
            Write-Host "[DryRun] upstream と同期" -ForegroundColor Gray
        }
    }
    finally {
        Pop-Location
    }
}
#endregion

#region Step 6: Create WinGet Manifest
if (-not $SkipWinGetPR) {
    Write-Banner "Step 6: Create WinGet Manifest"

    $installerUrl = "https://github.com/$GitHubOwner/$GitHubRepo/releases/download/$tagName/$zipName"
    $manifestVersion = '1.10.0'
    $releaseDate = (Get-Date).ToString('yyyy-MM-dd')

    # マニフェストディレクトリ
    $manifestDir = Join-Path $wingetPkgsPath "manifests/n/$Publisher/$PackageName/$Version"

    # version マニフェスト
    $versionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.$manifestVersion.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: $manifestVersion
"@

    # installer マニフェスト
    $installerManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$manifestVersion.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
InstallerType: zip
NestedInstallerType: portable
NestedInstallerFiles:
- RelativeFilePath: gistget.bat
  PortableCommandAlias: gistget
UpgradeBehavior: install
Commands:
- gistget
ReleaseDate: $releaseDate
InstallationMetadata:
  Files:
  - RelativeFilePath: gistget.bat
    FileType: launch
  - RelativeFilePath: GistGet.exe
    FileType: other
  - RelativeFilePath: Microsoft.Management.Deployment.CsWinRTProjection.dll
    FileType: other
  - RelativeFilePath: Microsoft.Management.Deployment.dll
    FileType: other
  - RelativeFilePath: Microsoft.Management.Deployment.winmd
    FileType: other
  - RelativeFilePath: WinRT.Runtime.dll
    FileType: other
Installers:
- Architecture: x64
  InstallerUrl: $installerUrl
  InstallerSha256: $sha256
ManifestType: installer
ManifestVersion: $manifestVersion
"@

    # defaultLocale マニフェスト
    $localeManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.$manifestVersion.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
PackageLocale: en-US
Publisher: $Publisher
PublisherUrl: https://github.com/$GitHubOwner
PackageName: $PackageName
PackageUrl: https://github.com/$GitHubOwner/$GitHubRepo
License: MIT
LicenseUrl: https://github.com/$GitHubOwner/$GitHubRepo/blob/main/LICENSE
ShortDescription: Windows Package Manager Cloud Sync Tool
Description: |-
  GistGet is a tool that synchronizes your WinGet package list with GitHub Gist,
  enabling easy backup and restoration of your installed packages across multiple machines.
Moniker: gistget
Tags:
- winget
- gist
- sync
- package-manager
- backup
ReleaseNotesUrl: https://github.com/$GitHubOwner/$GitHubRepo/releases/tag/$tagName
ManifestType: defaultLocale
ManifestVersion: $manifestVersion
"@

    Write-Host "マニフェストディレクトリ: $manifestDir" -ForegroundColor Yellow

    if (-not $DryRun) {
        # ディレクトリ作成
        if (Test-Path $manifestDir) {
            Remove-Item -Recurse -Force $manifestDir
        }
        New-Item -ItemType Directory -Path $manifestDir -Force | Out-Null

        # マニフェストファイルの書き込み
        $versionManifest | Set-Content -Path (Join-Path $manifestDir "$PackageIdentifier.yaml") -Encoding UTF8 -NoNewline
        $installerManifest | Set-Content -Path (Join-Path $manifestDir "$PackageIdentifier.installer.yaml") -Encoding UTF8 -NoNewline
        $localeManifest | Set-Content -Path (Join-Path $manifestDir "$PackageIdentifier.locale.en-US.yaml") -Encoding UTF8 -NoNewline

        Write-Host "マニフェストファイルを作成しました。" -ForegroundColor Green
    } else {
        Write-Host "[DryRun] マニフェストファイルを作成" -ForegroundColor Gray
        Write-Host ""
        Write-Host "--- $PackageIdentifier.yaml ---" -ForegroundColor Yellow
        Write-Host $versionManifest -ForegroundColor Gray
        Write-Host ""
        Write-Host "--- $PackageIdentifier.installer.yaml ---" -ForegroundColor Yellow
        Write-Host $installerManifest -ForegroundColor Gray
        Write-Host ""
        Write-Host "--- $PackageIdentifier.locale.en-US.yaml ---" -ForegroundColor Yellow
        Write-Host $localeManifest -ForegroundColor Gray
    }
}
#endregion

#region Step 7: Create Branch and Push
if (-not $SkipWinGetPR) {
    Write-Banner "Step 7: Create Branch and Push"

    Push-Location $wingetPkgsPath
    try {
        if (-not $DryRun) {
            # 既存のブランチがあれば削除
            $existingBranch = git branch --list $branchName
            if ($existingBranch) {
                Write-Host "既存のブランチを削除: $branchName" -ForegroundColor Yellow
                git branch -D $branchName
            }

            # 新しいブランチを作成
            Write-Host "ブランチを作成: $branchName" -ForegroundColor Yellow
            git checkout -b $branchName

            # 変更をステージング
            git add "manifests/n/$Publisher/$PackageName/$Version"

            # コミット
            $commitMessage = "New version: $PackageIdentifier version $Version"
            Write-Host "コミット: $commitMessage" -ForegroundColor Yellow
            git commit -m $commitMessage

            # fork リポジトリにプッシュ
            Write-Host "fork リポジトリにプッシュ中..." -ForegroundColor Yellow
            git push origin $branchName --force

            Write-Host "ブランチをプッシュしました: $branchName" -ForegroundColor Green
        } else {
            Write-Host "[DryRun] ブランチを作成: $branchName" -ForegroundColor Gray
            Write-Host "[DryRun] fork リポジトリにプッシュ" -ForegroundColor Gray
        }
    }
    finally {
        Pop-Location
    }
}
#endregion

#region Step 8: Create WinGet PR
if (-not $SkipWinGetPR -and -not $SkipPRCreation) {
    Write-Banner "Step 8: Create WinGet PR"

    $prTitle = "New version: $PackageIdentifier version $Version"
    $prBody = @"
### Package Information
- Package: $PackageIdentifier
- Version: $Version
- Installer URL: https://github.com/$GitHubOwner/$GitHubRepo/releases/download/$tagName/$zipName

### Checklist
- [x] Have you signed the [Contributor License Agreement](https://cla.opensource.microsoft.com/microsoft/winget-pkgs)?
- [x] Have you checked that there aren't other open [pull requests](https://github.com/microsoft/winget-pkgs/pulls) for the same manifest update/creation?
- [x] This PR only modifies one manifest
- [x] Have you validated your manifest locally with ``winget validate --manifest <path>``?
- [x] Have you tested your manifest locally with ``winget install --manifest <path>``?

---
*This PR was automatically created by [GistGet](https://github.com/$GitHubOwner/$GitHubRepo) release pipeline.*
"@

    if (-not $DryRun) {
        Write-Host "WinGet PR を作成中..." -ForegroundColor Yellow

        # gh CLI で PR を作成
        $prUrl = gh pr create `
            --repo "$WinGetUpstreamOwner/$WinGetUpstreamRepo" `
            --head "${Publisher}:${branchName}" `
            --base master `
            --title $prTitle `
            --body $prBody 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "PR の作成に失敗しました: $prUrl"
            Write-Host ""
            Write-Host "手動で PR を作成してください:" -ForegroundColor Yellow
            Write-Host "  https://github.com/$WinGetUpstreamOwner/$WinGetUpstreamRepo/compare/master...${Publisher}:${WinGetUpstreamRepo}:${branchName}" -ForegroundColor Cyan
        } else {
            Write-Host "WinGet PR を作成しました: $prUrl" -ForegroundColor Green

            # GitHub Release に WinGet PR リンクを追記
            if (-not $SkipGitHubRelease) {
                Write-Host "GitHub Release に WinGet PR リンクを追記中..." -ForegroundColor Yellow
                $currentBody = gh release view $tagName --repo "$GitHubOwner/$GitHubRepo" --json body --jq '.body'
                $updatedBody = $currentBody + "`n`n### WinGet`n- [WinGet PR]($prUrl)"

                # UTF-8 BOMなしで更新（文字化け防止）
                $updateNotesFile = Join-Path $artifactsPath "release-notes-update.md"
                [System.IO.File]::WriteAllText($updateNotesFile, $updatedBody, [System.Text.UTF8Encoding]::new($false))
                gh release edit $tagName --repo "$GitHubOwner/$GitHubRepo" --notes-file $updateNotesFile
                Remove-Item $updateNotesFile -ErrorAction SilentlyContinue

                Write-Host "GitHub Release を更新しました。" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "[DryRun] WinGet PR を作成" -ForegroundColor Gray
        Write-Host "[DryRun] Title: $prTitle" -ForegroundColor Gray
        Write-Host "[DryRun] Head: ${Publisher}:${branchName}" -ForegroundColor Gray
        Write-Host "[DryRun] Base: master" -ForegroundColor Gray
    }
}
#endregion

#region Summary
Write-Banner "Release Pipeline Complete"

Write-Host ""
Write-Host "サマリー:" -ForegroundColor Yellow
Write-Host "  バージョン: $Version" -ForegroundColor White
Write-Host "  タグ: $tagName" -ForegroundColor White
Write-Host "  SHA256: $sha256" -ForegroundColor White
Write-Host ""

if (-not $DryRun) {
    Write-Host "リリース URL:" -ForegroundColor Yellow
    Write-Host "  GitHub Release: https://github.com/$GitHubOwner/$GitHubRepo/releases/tag/$tagName" -ForegroundColor Cyan
    if (-not $SkipWinGetPR) {
        Write-Host "  WinGet PR: https://github.com/$WinGetUpstreamOwner/$WinGetUpstreamRepo/pulls?q=is:pr+$PackageIdentifier" -ForegroundColor Cyan
    }
} else {
    Write-Host "[DryRun] 実際のリリースは行われませんでした。" -ForegroundColor Yellow
    Write-Host "[DryRun] -DryRun オプションを外して再実行してください。" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "完了!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
#endregion
