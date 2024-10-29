@{
    # モジュールの一般情報
    RootModule = 'GistGet.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'ca1583d5-836a-46bc-a39d-fa11d205b864'
    Author = 'nuits.jp'
    Copyright = '(c) nuits.jp. All rights reserved.'
    Description = 'PowerShell module to manage WinGet package lists on Gist or Web or File.'
    PowerShellVersion = '7.0'

    # コマンドとエイリアスのエクスポート
    FunctionsToExport = @(
        'Set-GitHubToken',
        'Sync-GistGetPackage',
        'Install-GistGetPackage',
        'Uninstall-GistGetPackage',
        'Get-GistFile',
        'Set-GistFile'
    )
    CmdletsToExport = @()
    AliasesToExport = @()
    VariablesToExport = @()

    # スクリプトとアセンブリ
    ScriptsToProcess = @('Classes.ps1')
    RequiredAssemblies = @()

    # ディペンデンシー
    RequiredModules = @(
        'powershell-yaml',
        'PowerShellForGitHub',
        'Microsoft.WinGet.Client'
    )

    # 言語とフォーマット
    FileList = @()
    FormatsToProcess = @()
    TypesToProcess = @()

    # モジュールの互換性
    CompatiblePSEditions = @('Core')

    # ヘルプドキュメント
    HelpInfoURI = 'https://github.com/nuitsjp/GistGet'

    # Private data (署名や公開しないデータ)
    PrivateData = @{
        PSData = @{
            Tags = @('WinGet', 'GitHub', 'Gist', 'Package', 'Management')
            LicenseUri  = 'https://github.com/nuitsjp/GistGet/blob/main/LICENSE'
            ProjectUri  = 'https://github.com/nuitsjp/GistGet'
            ReleaseNotes = 'Initial release of GistGet PowerShell module for managing WinGet packages.'
        }
    }
}
