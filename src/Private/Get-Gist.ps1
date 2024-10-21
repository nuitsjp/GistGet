function Get-Gist {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $GistId,
        [string] $GistFileName
    )

    # Get Gist information
    Write-Verbose "Getting Gist for $GistId"
    $gist = Get-GitHubGist -Gist $GistId

    $fileName = $GistFileName
    if (-not $fileName) {
        # Get the first file if GistFileName is not specified
        $fileName = $gist.files.PSObject.Properties.Name | Select-Object -First 1
    }

    # Get file contents
    return $gist.files.$fileName.content
}