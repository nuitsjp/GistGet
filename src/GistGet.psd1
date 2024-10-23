@{
    # モジュールの一般情報
    RootModule = 'GistGet.psm1'
    ModuleVersion = '0.0.4'
    GUID = 'ca1583d5-836a-46bc-a39d-fa11d205b864'
    Author = 'nuits.jp'
    Copyright = '(c) nuits.jp. All rights reserved.'
    Description = 'PowerShell module to manage WinGet package lists on GitHub or Gist.'
    PowerShellVersion = '7.0'

    # コマンドとエイリアスのエクスポート
    FunctionsToExport = @(
        'Import-GistGetPackage',
        'Set-GitHubToken',
        'Set-GistGetGistId'
    )
    CmdletsToExport = @()
    AliasesToExport = @()
    VariablesToExport = @()

    # スクリプトとアセンブリ
    ScriptsToProcess = @()
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
            LicenseUri  = 'https://github.com/nuitsjp/GistGet/blob/main/LICENSE'
            ProjectUri  = 'https://github.com/nuitsjp/GistGet'
            ReleaseNotes = 'Initial release of GistGet PowerShell module for managing WinGet packages.'
        }
    }
}
