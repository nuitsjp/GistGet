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
    if($Uri)
    {
        Write-Verbose "Getting Gist from $Uri"
        $yaml = Invoke-RestMethod -Uri $Uri
    }

    return ConvertTo-GistGetPackageFromYaml -Yaml $yaml
}