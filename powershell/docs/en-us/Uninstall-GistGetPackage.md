# Install-GistGetPackage

Uninstalls packages and updates the YAML definition file on Gist.

```pwsh
Uninstall-GistGetPackage -Id Git.Git
```

Before installation:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
```

After installation:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
  uninstall: true
```

uninstall: true is marked in the YAML. This means that when Sync-GistGetPackage is called from another terminal, if the package is installed on that target terminal, it will be uninstalled, synchronizing the package state.

If you want to uninstall only locally without updating the definition file, use WinGet directly.

```pwsh
winget uninstall --id Git.Git
```

## Parameters

Install-GistGetPackage internally operates as follows:

1. Calls Get-WinGetPackage to retrieve packages
2. Calls Uninstall-WinGetPackage to install packages

Therefore, Uninstall-GistGetPackage can generally use the parameters of these two Functions.

The available parameters are shown below. For more details, please also refer to the help for Get-WinGetPackage and Uninstall-WinGetPackage.

|Parameter|Usage|Description|
|--|--|--|
|Query|Get|Specifies a string to search for packages. Matches against PackageIdentifier, PackageName, Moniker, and Tags.|
|Command|Get|Specifies the command name defined in the package manifest.|
|Count|Get|Limits the number of items returned.|
|Id|Get|Specifies the package ID. By default, performs a case-insensitive partial match search.|
|MatchOption|Get|Specifies the matching logic for searches. Can be 'Equals', 'EqualsCaseInsensitive', 'StartsWithCaseInsensitive', 'ContainsCaseInsensitive'.|
|Moniker|Get|Specifies the package moniker. For example: 'pwsh' for PowerShell.|
|Name|Get|Specifies the package name. Must be enclosed in quotes if it contains spaces.|
|Source|Get|Specifies the WinGet source name.|
|Tag|Get|Searches by package tag.|
|Force|Uninstall|Forces the uninstallation to execute.|
|Mode|Uninstall|Specifies the output mode for the uninstaller. Can be 'Default', 'Silent', 'Interactive'.|