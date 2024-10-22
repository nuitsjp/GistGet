function Set-GistGetGistId {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$GistId
    )

    # ユーザー環境変数「GistGetGistId」にGistIdを設定
    Set-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -Value $GistId
}
