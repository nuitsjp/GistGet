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
    if (-not $GistId -and -not $Path) {
        # Get GistId from environment variable
        Write-Verbose "Getting GistId from environment variable"
        $GistId = Get-GistGetGistId
        Write-Verbose "Environment variable GistId: $GistId"

        if (-not $GistId) {
            throw "Please specify a GistId or Path. Alternatively, you need to register the GistId in advance using Set-GistGetGistId."
        }
    }

    $yaml = [GistGetPackage]::ToYaml($Packages)

    if ($Path) {
        # Save to file
        $yaml | Set-Content -Path $Path -NoNewline
    }
    elseif($GistId)
    {
        $packageParams = @{}
        if ($GistId) { $packageParams['GistId'] = $GistId }
        if ($GistFileName) { $packageParams['GistFileName'] = $GistFileName }

        Set-Gist @packageParams -Content $yaml
    }
    else {
        # Should not reach here
        throw
    }
}