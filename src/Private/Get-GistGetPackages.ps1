function Get-GistGetPackages {
    [CmdletBinding()]
    param(
        [string] $GistId,
        [string] $GistFileName,
        [string] $Uri,
        [string] $Path
    )

    if ($Path) {
        $yaml = Get-Content -Path $Path -Raw
    }

    return ConvertTo-GistGetPackageFromYaml -Yaml $yaml
}