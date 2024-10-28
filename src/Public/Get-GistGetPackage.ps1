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
    elseif($GistFile)
    {
        $gistId = $GistFile.Id
        $gistFileName = $GistFile.FileName
        Write-Verbose "Getting Gist for id:$gistId fileName:$gistFileName"
        $remoteGist = Get-GitHubGist -Gist $gistId
        $yaml = $remoteGist.files.$gistFileName.content
    }
    else {
        throw "Please specify a GistId, Uri or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
    }

    return [GistGetPackage]::ParseYaml($yaml)
}