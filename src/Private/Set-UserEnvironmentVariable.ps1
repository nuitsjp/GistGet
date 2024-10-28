function Set-UserEnvironmentVariable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [string] $Value
    )

    [System.Environment]::SetEnvironmentVariable($Name, $Value, [System.EnvironmentVariableTarget]::User)
}