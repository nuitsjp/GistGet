# YAML→GistGetPackageクラスへの変換関数
class GistGetPackage {
    [string]$Id
    [string]$PackageParameters
    [bool]$Uninstall

    GistGetPackage([string]$id, [string]$packageParameters, [bool]$uninstall) {
        $this.Id = $id
        $this.PackageParameters = $packageParameters
        $this.Uninstall = $uninstall
    }
}
