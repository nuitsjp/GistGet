# Sync-GistGetPackage

Synchronizes (installs/uninstalls) packages using GistGet's YAML definition file.

```pwsh
Sync-GistGetPackage
```

By default, it uses the first file from a Gist that has only "GistGet" written in its "Gist description...".

In GistGet, you can retrieve and use the definition file from either a [GistFile](#Gist), [Uri](#Uri), or [File](#File).

## YAML

The YAML file is written as follows. [See here for detailed YAML definition.](YAML-Definition.md)

```yaml
7zip.7zip:
Microsoft.VisualStudioCode.Insiders:
  custom: /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Zoom.Zoom:
  uninstall: true
```

The id uses WinGet's ID.

```pwsh
PS D:\GistGet> winget search 7zip
Name              ID                     Version          Match          Source
--------------------------------------------------------------------------------
7-Zip             7zip.7zip              24.08              Moniker: 7zip winget
```

By specifying custom, you can pass additional parameters to the installer.

Also, if you set uninstall to true, when running Sync-GistGetPackage, the target package will be uninstalled if it's installed on that terminal.

When using [Uninstall-GistGetPackage](Uninstall-GistGetPackage.md), uninstall: true is automatically set during uninstallation.

This is particularly convenient compared to WinGet's import feature.

## Gist

By default, it uses the first file from a Gist that has only "GistGet" written in its "Gist description...".

```pwsh
Sync-GistGetPackage
```

You can also explicitly specify the Gist Id and File.

```pwsh
Set-GistFile -GistId 49990de4389f126d1f6d57c10c408a0c -File GistGet.yml
Sync-GistGetPackage
```

If Id and File are not set, it will search for the target YAML from the Description, so specifying them improves the experience slightly.

## Uri

You can specify a YAML file published on the web.

```pwsh
Sync-GistGetPackage -Uri https://gist.githubusercontent.com/nuitsjp/49990de4389f126d1f6d57c10c408a0c/raw/73583e15d292e3a461abebc548a3e6820046e81a/GistGet.yml
```

Of course, you can specify sources other than Gist in the Uri.

## File

For example, you can register files in a git repository and use them for synchronization.

```pwsh
Sync-GistGetPackage -Path .\GistGet.yml
```

This would be useful when managing packages used by specific products.

## Parameters

|Parameter|Description|
|--|--|
|Uri|Specifies the Gist URL containing the GistGet package configuration. If not specified, the first Gist with the description "GistGet" is used.|
|Path|Specifies the path to a local file containing the GistGet package configuration. The Uri parameter takes precedence.|