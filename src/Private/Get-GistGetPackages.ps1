function Get-GistGetPackages {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
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
    elseif($GistId)
    {
        # Get Gist information
        Write-Verbose "Getting Gist for $GistId"
        $gist = Get-GitHubGist -Gist $GistId

        $fileName = $GistFileName
        if (-not $fileName) {
            # Get the first file if GistFileName is not specified
            $fileName = $gist.files.PSObject.Properties.Name | Select-Object -First 1
        }

        # Get file contents
        $yaml = $gist.files.$fileName.content
    }
    else {
        throw "GistId, Uri, or Path must be specified"
    }

    return ConvertTo-GistGetPackageFromYaml -Yaml $yaml
}