# Set-GitHubToken

Gistの更新を行う場合、認証トークンを設定します。

```pwsh
Set-GitHubToken "<Your Access Token>"
```

トークンは、GitHubから公開されているPowerShell Module「PowerShellForGitHub」の「Set-GitHubAuthentication」を使って安全に保存されています。

```
$secureString = ($Token | ConvertTo-SecureString -AsPlainText -Force)
$cred = New-Object System.Management.Automation.PSCredential "username is ignored", $secureString
Set-GitHubAuthentication -Credential $cred
```

トークンは下記のリンクから発行します。

- [https://github.com/settings/personal-access-tokens/new](https://github.com/settings/personal-access-tokens/new)

トークンには、最低限の権限を割り当ててください。下記の権限があればGistGetは利用できます。

- Public Repositories(read-only)
- Account permissions - Gists Read and write

PowerShellForGitHubを別の用途でも利用するようであれば、必要な権限を割り当ててください。

具体的な設定は、つぎの通りです。

![](../images/repository-access.png)


![](../images/account-permissions.png)

必要な権限を割り当てたらトークンを発行し、つぎのように呼び出して設定します。

```pwsh
Set-GitHubToken "<Your Access Token>"
```
