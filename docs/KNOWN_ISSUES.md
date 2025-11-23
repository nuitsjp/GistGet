# Known Issues

## Winget Dependency
-   GistGet requires `winget` (Windows Package Manager) to be installed and available in the system PATH.
-   On some server environments (like Windows Server), `winget` might not be available by default.

## Authentication
-   Device Flow requires a browser to complete the authentication process.

## Sync Limitations
-   Currently, `sync` processes packages sequentially. Parallel installation is not yet supported.
-   System reboot requests from `winget` are reported but not automatically executed.

## COM API
-   The tool relies on the Windows Package Manager COM API. Changes to this API by Microsoft may impact functionality.
