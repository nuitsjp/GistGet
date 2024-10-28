function Set-GistGetPackages {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [Gist] $Gist,
        [Parameter(Mandatory = $true)]
        [GistGetPackage[]]$Packages
    )

    $yaml = [GistGetPackage]::ToYaml($Packages)
    Set-GistContent -Gist $Gist -Content $yaml
}