# GistGet

**GistGet** is a CLI tool designed to synchronize your Windows Package Manager (`winget`) packages across multiple devices using GitHub Gist. It allows you to maintain a consistent set of installed applications and tools, backed by a simple YAML configuration file stored in your private or public Gist.

## Features

-   **Cloud Synchronization**: Sync your installed packages via GitHub Gist.
-   **Winget Passthrough**: Use `gistget` as a wrapper for `winget` commands (e.g., `gistget search`, `gistget install`).
-   **Cross-Device**: Keep your work and home computers in sync.
-   **Configuration as Code**: Manage your software list in a readable `packages.yaml` format.

## Installation

### From GitHub Releases

1.  Download the latest release from the [Releases page](https://github.com/nuitsjp/GistGet/releases).
2.  Extract the zip file.
3.  Add the extracted folder to your system `PATH`.

### From Winget (Coming Soon)

```powershell
winget install nuitsjp.GistGet
```

## Usage

### Authentication

First, log in to your GitHub account to enable Gist access.

```powershell
gistget auth login
```

Follow the on-screen instructions to authenticate using the Device Flow.

### Synchronization

To synchronize your local packages with your Gist:

```powershell
gistget sync
```

This will:
1.  Fetch the `packages.yaml` from your Gist.
2.  Compare it with your locally installed packages.
3.  Install missing packages and uninstall packages marked for removal.

### Export / Import

To export your current state to a YAML file:

```powershell
gistget export --output my-packages.yaml
```

To import a YAML file to your Gist:

```powershell
gistget import my-packages.yaml
```

### Winget Commands

You can use `gistget` just like `winget`. It passes commands through to the underlying `winget` executable.

```powershell
gistget search vscode
gistget show Microsoft.PowerToys
```

## Configuration

GistGet uses a `packages.yaml` file in your Gist.

```yaml
Microsoft.PowerToys:
  version: 0.75.0
Microsoft.VisualStudioCode:
  custom: /VERYSILENT
DeepL.DeepL:
  uninstall: true
```

## Requirements

-   Windows 10/11
-   Windows Package Manager (`winget`)

## License

MIT License
