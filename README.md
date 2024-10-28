# GistGet

| パラメータ名 | パラメータ型 | Find-WinGetPackage | Install-WinGetPackage | Update-WinGetPackage |
|------------|-------------|-------------------|---------------------|-------------------|
| Query | String[] | オプション | オプション | オプション |
| Id | String | オプション | オプション | オプション |
| Name | String | オプション | オプション | オプション |
| Source | String | オプション | オプション | オプション |
| Moniker | String | オプション | オプション | オプション |
| MatchOption | Enum | オプション | オプション | オプション |
| Command | String | オプション | - | - |
| Count | UInt32 | オプション | - | - |
| Tag | String | オプション | - | - |
| AllowHashMismatch | SwitchParameter | - | オプション | オプション |
| Architecture | Enum | - | オプション | オプション |
| Custom | String | - | オプション | オプション |
| Force | SwitchParameter | - | オプション | オプション |
| Header | String | - | オプション | オプション |
| InstallerType | Enum | - | オプション | オプション |
| Locale | String | - | オプション | オプション |
| Location | String | - | オプション | オプション |
| Log | String | - | オプション | オプション |
| Mode | Enum | - | オプション | オプション |
| Override | String | - | オプション | オプション |
| PSCatalogPackage | PSObject | - | オプション | オプション |
| Scope | Enum | - | オプション | オプション |
| SkipDependencies | SwitchParameter | - | オプション | オプション |
| Version | String | - | オプション | オプション |
| Confirm | SwitchParameter | - | オプション | オプション |
| WhatIf | SwitchParameter | - | オプション | オプション |
| IncludeUnknown | SwitchParameter | - | - | オプション |



7zip.7zip: {}
Adobe.Acrobat.Reader.64-bit: {}
Amazon.Kindle: {}
AntibodySoftware.WizTree: {}
CoreyButler.NVMforWindows: {}
CubeSoft.CubePDF: {}
CubeSoft.CubePDFUtility: {}
DeepL.DeepL: {}
dotPDN.PaintDotNet: {}
gerardog.gsudo: {}
Git.Git: {}
icsharpcode.ILSpy: {}
IrfanSkiljan.IrfanView: {}
JetBrains.Rider: {}
JetBrains.Toolbox: {}
LINQPad.LINQPad.7: {}
Microsoft.AzureCLI: {}
Microsoft.DevHome: {}
Microsoft.PowerShell: {}
Microsoft.PowerToys: {}
Microsoft.SQLServerManagementStudio: {}
Microsoft.VisualStudio.2022.Enterprise.Preview: {}
Microsoft.VisualStudioCode.Insiders:
  override: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
Microsoft.WindowsTerminal: {}
NickeManarin.ScreenToGif: {}
NuitsJp.ClaudeToZenn:
  uninstall: true
OpenJS.NodeJS: {}
SlackTechnologies.Slack: {}
voidtools.Everything: {}
WinMerge.WinMerge: {}
Zoom.Zoom: {}
