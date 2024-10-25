# Setting Up Gist Authentication Token

[for Japanese](ja-jp/Set-GitHubToken.md)

To perform Git updates, you need to set up an authentication token.

You can generate a token from the link below:

- [https://github.com/settings/personal-access-tokens/new](https://github.com/settings/personal-access-tokens/new)

Assign the minimum necessary permissions to the token.

Here, we assign the minimum permissions needed to use GistGet. If you intend to use PowerShellForGitHub for other purposes, make sure to assign the appropriate permissions.

For repository access, assign the lowest level of permission, which is read access to public repositories.

![](../images/repository-access.png)

For Account permissions, assign Git Read and Write access.

![](../images/account-permissions.png)

After assigning the required permissions, generate the token and set it up as shown below:

```pwsh
Set-GitHubToken -Token "<Your Access Token>"
```

