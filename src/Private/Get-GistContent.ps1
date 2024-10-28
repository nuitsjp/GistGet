function Get-GistContent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [GistFile] $Gist
    )

    # Get Gist information
    $gistId = $Gist.Id
    $gistFileName = $Gist.FileName
    Write-Verbose "Getting Gist for id:$gistId fileName:$gistFileName"
    $remoteGist = Get-GitHubGist -Gist $gistId
    $file = $remoteGist.files.$gistFileName
    if (-not $file) {
        throw "The file $gistFileName was not found in the Gist with id $gistId."
    }   

    return $file.content
}