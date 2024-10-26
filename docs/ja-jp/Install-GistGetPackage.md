# Install-GistGetPackage

パッケージをインストールし、あわせてGist上のYAML定義ファイルを更新します。

```pwsh
Install-GistGetPackage -Id Git.Git
```

インストール前:

```yaml
- id: 7zip.7zip
- id: Adobe.Acrobat.Reader.64-bit
```

インストール後:

```yaml
- id: 7zip.7zip
- id: Adobe.Acrobat.Reader.64-bit
- id: Git.Git
```

定義ファイルを更新せず、ローカルへのみインストールしたい場合はWinGetを直接利用してください。

```pwsh
winget install --id Git.Git
```

