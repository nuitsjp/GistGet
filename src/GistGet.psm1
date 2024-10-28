# GistGet.psm1

# エラーが発生した場合はスクリプトを停止
$ErrorActionPreference = 'Stop'

$global:EnvironmentVariableNameGistId = 'GIST_GET_GIST_ID'
$global:EnvironmentVariableNameGistFileName = 'GIST_GET_GIST_FILE_NAME'

# クラス定義を最初にロード
. $PSScriptRoot\Classes.ps1

# Public関数のロード
$Public = @( Get-ChildItem -Path $PSScriptRoot\Public\*.ps1 -ErrorAction SilentlyContinue )
$Private = @( Get-ChildItem -Path $PSScriptRoot\Private\*.ps1 -ErrorAction SilentlyContinue )

# Private関数のドット・ソーシング
foreach($import in $Private) {
    try {
        . $import.FullName
    }
    catch {
        Write-Error -Message "Failed to import function $($import.FullName): $_"
    }
}

# Public関数のドット・ソーシング
foreach($import in $Public) {
    try {
        . $import.FullName
    }
    catch {
        Write-Error -Message "Failed to import function $($import.FullName): $_"
    }
}

# Public関数のエクスポート
Export-ModuleMember -Function $Public.BaseName