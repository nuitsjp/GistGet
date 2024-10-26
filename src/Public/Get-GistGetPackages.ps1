<#
.SYNOPSIS
    Retrieves GistGet package information from various sources.

.DESCRIPTION
    This function retrieves package information for GistGet from one of three possible sources:
    - A GitHub Gist (using GistId)
    - A remote URL
    - A local file path
    
    If no source is specified, it attempts to retrieve the GistId from environment variables.
    The retrieved YAML content is then converted to GistGetPackage objects.

.PARAMETER GistId
    The ID of the Gist containing the package information.

.PARAMETER GistFileName
    The specific filename within the Gist to retrieve. If not specified and using GistId, 
    the first file in the Gist will be used.

.PARAMETER Uri
    The URL from which to retrieve the package information.

.PARAMETER Path
    The local file path from which to retrieve the package information.

.EXAMPLE
    PS> Get-GistGetPackage
    Retrieves package information using the GistId stored in environment variables.

.EXAMPLE
    PS> Get-GistGetPackage -GistId "1234567890abcdef" -GistFileName "packages.yaml"
    Retrieves package information from a specific file in the specified Gist.

.EXAMPLE
    PS> Get-GistGetPackage -Uri "https://example.com/packages.yaml"
    Retrieves package information from a remote URL.

.EXAMPLE
    PS> Get-GistGetPackage -Path "C:\packages.yaml"
    Retrieves package information from a local file.

.NOTES
    Priority order for source selection:
    1. Path (if specified)
    2. Uri (if specified)
    3. GistId (if specified)
    4. Environment variable GistId (if no other source specified)

    Requirements:
    - For Gist retrieval: GitHub authentication must be configured
    - For URL retrieval: Internet connectivity required
    - YAML content must be in the correct format for GistGetPackage conversion

.OUTPUTS
    [GistGetPackage[]]
    Returns an array of GistGetPackage objects parsed from the YAML content.

.LINK
    Get-GistGetGistId
    Get-GitHubGist
    ConvertTo-GistGetPackageFromYaml
#>
function Get-GistGetPackage {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Uri,
        [string] $Path
    )

    # If no arguments are specified, get GistId from environment variable
    if (-not $GistId -and -not $Uri -and -not $Path) {
        Write-Verbose "Getting GistId from environment variable"
        $GistId = Get-GistGetGistId
        Write-Verbose "Environment variable GistId: $GistId"
    }

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
        Write-Verbose "Getting Gist for $GistId"
        $gist = Get-GitHubGist -Gist $GistId

        $fileName = $GistFileName
        if (-not $fileName) {
            $fileName = $gist.files.PSObject.Properties.Name | Select-Object -First 1
        }

        $yaml = $gist.files.$fileName.content
    }
    else {
        throw "Please specify a GistId, Uri or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
    }

    return ConvertTo-GistGetPackageFromYaml -Yaml $yaml
}