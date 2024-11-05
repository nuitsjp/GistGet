# Install-GistGetPackage

Installs packages and updates the YAML definition file on Gist.

```pwsh
Install-GistGetPackage -Id Git.Git
```

Before installation:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
```

After installation:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
```

You can add parameters during installation.

```pwsh
Install-GistGetPackage `
  -Id Microsoft.VisualStudioCode.Insiders `
  -Custom "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
```

After installation:

```yaml
7zip.7zip:
Adobe.Acrobat.Reader.64-bit:
Git.Git:
Microsoft.VisualStudioCode.Insiders:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
```

If you want to install only locally without updating the definition file, use WinGet directly.

```pwsh
winget install --id Git.Git
```

## Parameters

Install-GistGetPackage internally operates as follows:

1. Calls Find-WinGetPackage to retrieve packages
2. Calls Install-WinGetPackage to install packages

Therefore, Install-GistGetPackage can generally use the parameters of these two Functions.

The available parameters are shown below. For more details, please also refer to the help for Find-WinGetPackage and Install-WinGetPackage.

|Parameter|Usage|Description|
|--|--|--|
|Query|Find|Specifies a string to search for packages. Matches against PackageIdentifier, PackageName, Moniker, and Tags.|
|Id|Find|Specifies the package identifier.|
|Name|Find|Specifies the package name.|
|Source|Find|Specifies the WinGet source to install packages from.|
|Moniker|Find|Specifies the package Moniker.|
|MatchOption|Find|Specifies the matching option for package search. (Equals, EqualsCaseInsensitive, StartsWithCaseInsensitive, ContainsCaseInsensitive)|
|AllowHashMismatch|Install|Allows download even if SHA256 hash of installer or dependencies doesn't match.|
|Architecture|Install|Specifies the processor architecture of the installer. (Default, X86, Arm, X64, Arm64)|
|Custom|Install|Passes additional arguments to the installer.|
|Force|Install|Forces installation by skipping normal checks.|
|Header|Install|Specifies custom HTTP header values to pass to WinGet REST source.|
|InstallerType|Install|Specifies the type of installer to use. (Default, Inno, Wix, Msi, Nullsoft, Zip, Msix, Exe, Burn, MSStore, Portable)|
|Locale|Install|Specifies the installer locale in BCP47 format (e.g., en-US).|
|Location|Install|Specifies the installation path for the package.|
|Log|Install|Specifies the path for the installer's log file.|
|Mode|Install|Specifies the execution mode for the installer. (Default, Silent, Interactive)|
|Override|Install|Overrides existing arguments passed to the installer.|
|Scope|Install|Specifies the installation scope. (Any, User, System, UserOrUnknown, SystemOrUnknown)|
|SkipDependencies|Install|Skips installation of dependencies.|
|Version|Install|Specifies the version of the package to install.|
|Confirm|Install|Displays confirmation prompt before execution.|
|WhatIf|Install|Shows what actions would be performed without actually executing them.|