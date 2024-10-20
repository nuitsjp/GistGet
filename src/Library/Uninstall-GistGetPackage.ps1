function Uninstall-GistGetPackage {
    [CmdletBinding()]
    param (
        [string[]]$Query,
        [string]$Command,
        [uint32]$Count,
        [string]$Id,
        [ValidateSet("Equals", "EqualsCaseInsensitive", "StartsWithCaseInsensitive", "ContainsCaseInsensitive")]
        [string]$MatchOption,
        [string]$Moniker,
        [string]$Name,
        [string]$Source,
        [string]$Tag
    )

    # Get-WinGetPackage用のハッシュテーブルを作成し、指定されたパラメーターのみ追加
    $packageParams = @{}
    if ($Query) { $packageParams['Query'] = $Query }
    if ($Command) { $packageParams['Command'] = $Command }
    if ($Count) { $packageParams['Count'] = $Count }
    if ($Id) { $packageParams['Id'] = $Id }
    if ($MatchOption) { $packageParams['MatchOption'] = $MatchOption }
    if ($Moniker) { $packageParams['Moniker'] = $Moniker }
    if ($Name) { $packageParams['Name'] = $Name }
    if ($Source) { $packageParams['Source'] = $Source }
    if ($Tag) { $packageParams['Tag'] = $Tag }

    # Get-WinGetPackageでパッケージのリストを取得
    $packages = Get-WinGetPackage @packageParams
    $packages

    # 取得したパッケージを削除
    # foreach ($package in $packages) {
    #     Write-Verbose "Removing package: $($package.Name)"
    #     if ($package.Id) {
    #         Remove-WinGetPackage -Id $package.Id -Confirm:$false
    #     } else {
    #         Write-Warning "Package does not have an Id and cannot be removed: $($package.Name)"
    #     }
    # }
}

