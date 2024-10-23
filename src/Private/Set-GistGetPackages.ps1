function Set-GistGetPackages {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [GistGetPackage[]]$Packages
    )

    # 引数がいずれも指定されていない場合は、環境変数から GistId を取得
    if (-not $GistId -and -and -not $Path) {
        # Get GistId from environment variable
        Write-Verbose "Getting GistId from environment variable"
        $GistId = Get-GistGetGistId
        Write-Verbose "Environment variable GistId: $GistId"
    }

    if ($Path) {
        Write-Verbose "Getting Gist from $Path"
        $yaml = Get-Content -Path $Path -Raw
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
        throw "Please specify a GistId or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
    }

    return ConvertTo-GistGetPackageFromYaml -Yaml $yaml
}