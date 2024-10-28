function Set-GistGetPackages {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [GistFile] $Gist,
        [Parameter(Mandatory = $true)]
        [GistGetPackage[]]$Packages
    )

    $yaml = [GistGetPackage]::ToYaml($Packages)
    Set-GistContent -Gist $Gist -Content $yaml
}