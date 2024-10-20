@{
    # モジュールの一般情報
    ModuleVersion = '0.0.1'
    GUID = 'ca1583d5-836a-46bc-a39d-fa11d205b864'
    Author = 'nuits.jp'
    Copyright = '(c) nuits.jp. All rights reserved.'
    Description = 'PowerShell module to manage WinGet package lists on GitHub or Gist.'
    PowerShellVersion = '7.0'

    # コマンドとエイリアスのエクスポート
    FunctionsToExport = @(
        'Import-GistGetPackage',
        'Get-GitHubPackageList', 
        'Add-GitHubPackage', 
        'Remove-GitHubPackage'
    )
    CmdletsToExport = @()
    AliasesToExport = @()
    VariablesToExport = @()

    # スクリプトとアセンブリ
    ScriptsToProcess = @()
    RequiredAssemblies = @()

    # ディペンデンシー
    RequiredModules = @(
        'PowerShellForGitHub',
        'Microsoft.WinGet.Client')
    ExternalModuleDependencies = @()

    # 言語とフォーマット
    FileList = @()
    FormatsToProcess = @()
    TypesToProcess = @()

    # DscResources and NestedModules are currently not used
    NestedModules = @()

    # 使い方やサンプルへのリンク（あれば）
    HelpInfoURI = 'https://github.com/nuitsjp/GistGet'

    # Private data (署名や公開しないデータ)
    PrivateData = @{}

    # モジュールの互換性
    CompatiblePSEditions = @('Core')  # PowerShell CoreとDesktopのどちらで使用可能か

    # モジュールの必要性や参照ドキュメント
    RequiredScripts = @()
    ExternalScripts = @()
    ExternalTypes = @()

    # ログ、署名に関する設定
    LogPipelineExecutionDetails = $false
    LicenseUri = 'https://github.com/nuitsjp/GistGet/blob/main/LICENSE'
    ProjectUri = 'https://github.com/nuitsjp/GistGet'
    ReleaseNotes = 'Initial release of GistGet PowerShell module for managing WinGet packages.'
}
