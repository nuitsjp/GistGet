# Set-GitHubToken

Gistの更新を行う場合、認証トークンを設定します。

```pwsh
Set-GitHubToken github_pat_11AD3NELA0SGEHcrynCMSo...
```

トークンは、GitHubから公開されているPowerShell Module「PowerShellForGitHub」の[「Set-GitHubAuthentication」を使って安全に保存されています](https://github.com/nuitsjp/GistGet/blob/37cc27b3cf0a23e63eb91497cadcdb5ccac9f66a/src/Public/Set-GitHubToken.ps1#L40)。

トークンは下記のリンクから発行します。

- [https://github.com/settings/personal-access-tokens/new](https://github.com/settings/personal-access-tokens/new)

トークンには、必要最低限の権限を割り当ててください。

PowerShellForGitHubをGistGetにしか利用しないのであれば、下記の権限を割り当ててください。

- Public Repositories(read-only)
- Account permissions - Gists Read and write

具体的な設定は、つぎの通りです。

![](../images/repository-access.png)


![](../images/account-permissions.png)

必要な権限を割り当てたらトークンを発行し、つぎのように呼び出して設定します。
