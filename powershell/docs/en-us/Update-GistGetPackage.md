# Update-GistGetPackage

Updates all installed packages.

This includes not only packages defined in GistGet but all packages installed locally.

```pwsh
Update-GistGetPackage
```

In GistGet, you can specify package versions.

```yaml
7zip.7zip:
  version: 1.0.0
```

The behavior changes depending on the combination of GistGet's version definition and the locally installed version.

|Local Version|Behavior|
|--|--|
|= 1.0.0|Will not update even if new versions are released.|
|≠ 1.0.0|After confirmation, replaces with version 1.0.0.|
|Not installed|Will not install. Use Sync-GistGetPackage to install.|

# Parameters

None