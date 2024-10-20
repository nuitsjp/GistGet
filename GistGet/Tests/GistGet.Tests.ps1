# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot/../GistGet.psm1" -Force

Describe "GistGetPackage Tests" {
    It "Test for ConvertTo-GistGetPackageFromYaml" {
        # Arrange: テストの準備
        $yaml = Get-Content -Path "$PSScriptRoot\assets\test.yaml" -Raw
        
        # Act: 関数を実行
        $packages = ConvertTo-GistGetPackageFromYaml -Yaml $yaml
        
        # Assert: 結果が期待通りか確認
        $packages.Count | Should -Be 3
        $packages[0].Id | Should -Be "7zip.7zip"
        $packages[0].PackageParameters | Should -Be ""
        $packages[0].Uninstall | Should -Be $false

        $packages[1].Id | Should -Be "Microsoft.VisualStudioCode.Insiders"
        $packages[1].PackageParameters | Should -Be "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
        $packages[1].Uninstall | Should -Be $false

        $packages[2].Id | Should -Be "Zoom.Zoom"
        $packages[2].PackageParameters | Should -Be ""
        $packages[2].Uninstall | Should -Be $true
    }

    It "Test for ConvertTo-YamlFromGistGetPackage" {
        # Arrange: テストの準備
        $yaml = Get-Content -Path "$PSScriptRoot\assets\test.yaml" -Raw
        $packages = ConvertTo-GistGetPackageFromYaml -Yaml $yaml

        
        # Act: 関数を実行
        $result = ConvertTo-YamlFromGistGetPackage -Packages $packages
        
        # Assert: 結果が期待通りか確認
        $result | Should -Be "- id: 7zip.7zip
- id: Microsoft.VisualStudioCode.Insiders
  packageParameters: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath
- id: Zoom.Zoom
  uninstall: true
"
    }
}
