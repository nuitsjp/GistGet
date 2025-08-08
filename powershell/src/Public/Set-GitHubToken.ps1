<#
.SYNOPSIS
    Sets up GitHub authentication token.

.DESCRIPTION
    Securely sets up a GitHub Personal Access Token for authentication.
    The token is stored in an encrypted format and safely cleared from memory after authentication.

.PARAMETER Token
    Specifies the GitHub Personal Access Token string.

.EXAMPLE
    PS> Set-GitHubToken -Token "ghp_xxxxxxxxxxxxxxxx"
    Sets up GitHub authentication using the provided token.

.NOTES
    Security considerations:
    - Token is converted to SecureString and never stored in plain text
    - Memory is properly cleared after use
    - Credentials are handled securely through Set-GitHubAuthentication

.LINK
    Set-GitHubAuthentication

.INPUTS
    System.String

.OUTPUTS
    None
#>
function Set-GitHubToken {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = $true)]
        [string] $Token
    )

    $secureString = ($Token | ConvertTo-SecureString -AsPlainText -Force)
    $cred = New-Object System.Management.Automation.PSCredential "username is ignored", $secureString
    Set-GitHubAuthentication -Credential $cred
    $Token = $null
    
    $secureString = $null
    $cred = $null
}