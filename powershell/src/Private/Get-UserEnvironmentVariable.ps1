function Get-UserEnvironmentVariable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    return (Get-ItemProperty -Path 'HKCU:\Environment').$Name
}