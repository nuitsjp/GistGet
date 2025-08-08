# YAML Definition

Here's an example of the YAML definition:

```yaml
7zip.7zip:
Microsoft.VisualStudioCode:
  allowHashMismatch: true
  architecture: x64
  custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders
  force: true
  header: 'Authorization: Bearer xxx'
  installerType: exe
  locale: en-US
  location: C:\Program Files\Microsoft VS Code
  log: C:\Temp\vscode_install.log
  mode: silent
  override: /SILENT
  scope: machine
  skipDependencies: true
  version: 1.85.0
  confirm: true
  whatIf: true
  uninstall: false
```

## Parameter List
- [allowHashMismatch](#allowhashmatch)
- [architecture](#architecture)
- [custom](#custom)
- [force](#force)
- [header](#header)
- [installerType](#installertype)
- [locale](#locale)
- [location](#location)
- [log](#log)
- [mode](#mode)
- [override](#override)
- [scope](#scope)
- [skipDependencies](#skipdependencies)
- [version](#version)
- [confirm](#confirm)
- [whatIf](#whatif)
- [uninstall](#uninstall)

## Parameters

### allowHashMismatch
Allows download even when the SHA256 hash of the installer or dependencies doesn't match the hash in the WinGet package manifest.

### architecture
Specifies the processor architecture for the WinGet package installer.
Available values:
- Default
- X86
- Arm
- X64
- Arm64

### custom
Used to pass additional arguments to the installer. When including multiple arguments, they must be included in the string in the format expected by the installer.

### force
Skips normal WinGet checks and forces the installer to run.

### header
Specifies custom HTTP header values to pass to the WinGet REST source.

### installerType
Specifies the package installer type.
Available values:
- Default
- Inno
- Wix
- Msi
- Nullsoft
- Zip
- Msix
- Exe
- Burn
- MSStore
- Portable

### locale
Specifies the installer package locale. Must be specified in BCP 47 format (e.g., en-US).

### location
Specifies the file path where the package will be installed. The installer must support alternative installation locations.

### log
Specifies the location for the installer log file. Must include either a fully qualified or relative path with filename.

### mode
Specifies the output mode for the installer.
Available values:
- Default
- Silent
- Interactive

### override
Overrides existing arguments passed to the installer. Specify a single string value that overrides the arguments specified in the package manifest.

### scope
Specifies the WinGet package installer scope.
Available values:
- Any
- User
- System
- UserOrUnknown
- SystemOrUnknown

### skipDependencies
Skips the installation of WinGet package dependencies.

### version
Specifies the version of the package to install.

### confirm
Displays a confirmation prompt before executing the cmdlet.

### whatIf
Shows what would happen if the cmdlet runs without actually executing it.

### uninstall
Specifies the uninstall state of the package. When set to true, the package will be uninstalled when Sync-GistGetPackage is executed.