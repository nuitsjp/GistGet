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
