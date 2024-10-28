function Set-UserEnvironmentVariable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [string] $Value
    )

    Set-ItemProperty -Path 'HKCU:\Environment' -Name $Name -Value $Value
}