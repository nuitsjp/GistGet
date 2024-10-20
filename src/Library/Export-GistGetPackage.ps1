function ExportGistGetPackage {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$GistId,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $gist = Get-Gist -Id $GistId

    $gist.Files | ForEach-Object {
        $file = $_
        $filePath = Join-Path -Path $OutputPath -ChildPath $file.Filename
        $file.Content | Set-Content -Path $filePath
    }
}