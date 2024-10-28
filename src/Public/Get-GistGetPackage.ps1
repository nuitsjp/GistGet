function Get-GistGetPackage {
    [CmdletBinding()]
    param(
        [Gist] $Gist,
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
    elseif($Gist)
    {
        $GistId = $Gist.Id
        $GistFileName = $Gist.FileName
        Write-Verbose "Getting Gist for id:$GistId fileName:$GistFileName"
        $remoteGist = Get-GitHubGist -Gist $GistId
        $yaml = $remoteGist.files.$GistFileName.content
    }
    else {
        throw "Please specify a GistId, Uri or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
    }

    return [GistGetPackage]::ParseYaml($yaml)
}