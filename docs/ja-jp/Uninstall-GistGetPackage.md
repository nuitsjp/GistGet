# Install-GistGetPackage

パッケージをアンインストールし、あわせてGist上のYAML定義ファイルを更新します。

```pwsh
Uninstall-GistGetPackage -Id Git.Git
```

インストール前:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
```

インストール後:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
  uninstall: true
```

YAML上にuninstall: trueがマークされます。これにより別の端末などからSync-GistGetPackageを呼び出されたときに、対象端末にパッケージがインストールされていた場合はアンインストールされ、パッケージの状態が同期されます。

定義ファイルを更新せず、ローカルのみアンインストールしたい場合はWinGetを直接利用してください。

```pwsh
winget uninstall --id Git.Git
```

## Parameters

Install-GistGetPackageは、内部的に次のように動作します。

1. Get-WinGetPackage を呼び出してパッケージを取得
2. Uninstall-WinGetPackage を呼び出してパッケージをインストール

したがってUninstall-GistGetPackageでは、それらの2つのFunctionのパラメーターがおおむね利用できます。

利用できるパラメーターをつぎに示します。詳細はGet-WinGetPackageとUninstall-WinGetPackageのヘルプもあわせてご覧ください。

|Parameter|用途|説明|
|--|--|--|
|Query|Get|パッケージを検索する文字列を指定します。PackageIdentifier、PackageName、Moniker、Tagsに対してマッチングを行います。|
|Command|Get|パッケージマニフェストで定義されているコマンド名を指定します。|
|Count|Get|返される項目の数を制限します。|
|Id|Get|パッケージIDを指定します。デフォルトでは大文字小文字を区別しない部分一致検索を行います。|
|MatchOption|Get|検索時のマッチングロジックを指定します。'Equals'、'EqualsCaseInsensitive'、'StartsWithCaseInsensitive'、'ContainsCaseInsensitive'が指定可能です。|
|Moniker|Get|パッケージのmonikerを指定します。例：PowerShellの場合は'pwsh'。|
|Name|Get|パッケージ名を指定します。スペースを含む場合は引用符で囲む必要があります。|
|Source|Get|WinGetのソース名を指定します。|
|Tag|Get|パッケージのタグを指定して検索します。|
|Force|Uninstall|アンインストールを強制的に実行します。|
|Mode|Uninstall|アンインストーラーの出力モードを指定します。'Default'、'Silent'、'Interactive'が指定可能です。|
