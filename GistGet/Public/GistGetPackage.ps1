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

# GistGetPackageクラス→YAMLへの変換関数
function ConvertTo-YamlFromGistGetPackage {
    param (
        [GistGetPackage[]]$Packages
    )

    # uninstallがtrueの場合のみプロパティを保持して新しいオブジェクトを構築
    $yamlObjects = foreach ($pkg in $Packages) {
        $yamlObject = @{
            id = $pkg.Id
        }
        if ($pkg.Uninstall -eq $true) {
            $yamlObject.uninstall = $pkg.Uninstall
        }
        if ($pkg.PackageParameters) {
            $yamlObject.packageParameters = $pkg.PackageParameters
        }
        
        [PSCustomObject]$yamlObject
    }

    # YAML形式に変換してファイルに保存
    $yamlOutput = $yamlObjects | ConvertTo-Yaml
    return $yamlOutput
}
