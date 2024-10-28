function Set-GistGetPackages {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [GistFile] $GistFile,
        [Parameter(Mandatory = $true)]
        [GistGetPackage[]]$Packages
    )

    $yaml = [GistGetPackage]::ToYaml($Packages)
    Set-GistContent -GistFile $GistFile -Content $yaml
}