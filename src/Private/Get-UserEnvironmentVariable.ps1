function Get-UserEnvironmentVariable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    return [System.Environment]::GetEnvironmentVariable($Name, [System.EnvironmentVariableTarget]::User)
}