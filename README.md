# What is GistGet?

[Japanese](README.ja-jp.md)

GistGet is a PowerShell Module for managing WinGet installation lists on Gist.

Besides Gist, you can also use Uri or file paths, making it possible to set up development environments for products and synchronize development terminal settings.

It differs from WinGet's export/import in the following ways:

1. Definition files are designed to be handled on the cloud (Gist, Web) from the start
2. Parameters can be passed to the installer
3. Settings are synchronized with Gist during install/uninstall
4. Uninstallation can also be synchronized

# Table of Contents 

- [Getting started](#getting-started)
- [Functions](#functions)
- [YAML Definition](docs/en-us/YAML-Definition.md)

# Getting started

Install the Module from PowerShell Gallery.

```pwsh
Install-Module GistGet
```

[Get a token to update Gist from GitHub](https://github.com/settings/personal-access-tokens/new) and set it up. For required token permissions and other details, please refer to [here](https://github.com/nuitsjp/GistGet/blob/main/docs/en-us/Set-GitHubToken.md#permissions).

```pwsh
Set-GitHubToken github_pat_11AD3NELA0SGEHcrynCMSo...
```

Create an installation list in Gist.

**Set "GistGet" in the "Gist description..."** The filename is arbitrary.

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
```

Synchronize packages according to the Gist definition.

```pwsh
Sync-GistGetPackage
```

Update all packages (winget upgrade).

```pwsh
Update-GistGetPackage
```

Install a new package.

```pwsh
Install-GistGetPackage -Id Git.Git
```

When installing through GistGet commands, the definition file on Gist is also updated.

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Git.Git:
```

This makes it easy to synchronize environments by running Sync-GistGetPackage on another terminal.

Uninstall an installed package.

```pwsh
Uninstall-GistGetPackage -Id Git.Git
```

The definition file on Gist is also synchronized.

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Git.Git:
  uninstall: true
```

When you run Sync-GistGetPackage on another terminal, it will also be uninstalled from that terminal.

If you don't want to synchronize uninstallation, use WinGet's standard command.

```pwsh
winget uninstall --id Git.Git
```

# Functions

|Function|Overview|
|--|--|
|[Set-GitHubToken](docs/en-us/Set-GitHubToken.md)|Set GitHub token for retrieving and updating Gist definition of installation packages.|
|[Sync-GistGetPackage](docs/en-us/Sync-GistGetPackage.md)|Synchronize local packages with Gist definition.|
|[Update-GistGetPackage](docs/en-us/Update-GistGetPackage.md)|Synchronize local packages with Gist definition.|
|[Install-GistGetPackage](docs/en-us/Install-GistGetPackage.md)|Install package from WinGet and update the definition file on Gist.|
|[Uninstall-GistGetPackage](docs/en-us/Uninstall-GistGetPackage.md)|Uninstall package and mark uninstallation on Gist.|
|[Set-GistFile](docs/en-us/Set-GistFile.md)|Set Id or filename when you want to get Gist from Id or filename instead of Gist description.|
|[Get-GistFile](docs/en-us/Get-GistFile.md)|Get the configured Gist Id and other settings.|