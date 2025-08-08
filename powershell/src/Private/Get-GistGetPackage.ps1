function Get-GistGetPackage {
    [CmdletBinding()]
    param(
        [GistFile] $GistFile,
        [string] $Uri,
        [string] $Path
    )

    if ($Path) {
        Write-Verbose "Getting Gist from $Path"
        $yaml = Get-Content -Path $Path -Raw
    }
    elseif($Uri) {
        Write-Verbose "Getting Gist from $Uri"
        $yaml = Invoke-RestMethod -Uri $Uri
    }
    else {
        if (-not $GistFile) {
            $GistFile = Get-GistFile
        }
        $yaml = Get-GistContent -GistFile $GistFile
    }

    return [GistGetPackage]::ParseYaml($yaml)
}