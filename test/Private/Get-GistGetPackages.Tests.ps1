# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-GistGetPackages Tests" {
        It "From File" {
            # Arrange: テストの準備
            
            # Act: 関数を実行
            $packages = Get-GistGetPackages -Path "$PSScriptRoot\assets\test.yaml"
            
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

        It "From Uri" {
        # Arrange: テストの準備

        # Act: 関数を実行
        $packages = Get-GistGetPackages -Uri "https://gist.githubusercontent.com/nuitsjp/82b27147c684502b4f9c4488f9fe6fa9/raw/a31f9016194b09b016def634765af9dedb13023c/Test-Load-Packages.yaml"

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

        It "From GistId" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            $packages = Get-GistGetPackages -GistId "82b27147c684502b4f9c4488f9fe6fa9"
    
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

        It "From GistId and FileName" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            $packages = Get-GistGetPackages -GistId "82b27147c684502b4f9c4488f9fe6fa9" -GistFileName "Test-Get-GistGetPackages.yaml"
    
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

        It "Ocurred Error" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            { Get-GistGetPackages } | Should -Throw

            # Assert: 結果が期待通りか確認
        }

    }
}
