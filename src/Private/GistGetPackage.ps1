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
        $yamlObject = [ordered]@{
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

function ConvertTo-GistGetPackageFromYaml {
    param (
        [string]$Yaml
    )
    
    # YAMLを読み込み、PSCustomObjectに変換
    $customObjects = $Yaml | ConvertFrom-Yaml

    # オブジェクトをGistGetPackageクラスに変換
    $packages = foreach ($obj in $customObjects) {
        $uninstall = if ($null -ne $obj.uninstall) { [bool]$obj.uninstall } else { $false }
        [GistGetPackage]::new(
            $obj.id, 
            $obj.packageParameters, 
            $uninstall
        )
    }

    return $packages
}
