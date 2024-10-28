# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-GistGetPackage Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'FooBar'
            }
        }

        BeforeEach {
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }
        }

        It "From File" {
            # Arrange: テストの準備
            
            # Act: 関数を実行
            $packages = Get-GistGetPackage -Path "$PSScriptRoot\assets\test.yaml"
            
            # Assert: 結果が期待通りか確認
            $packages.Count | Should -Be 3
            $packages[0].Id | Should -Be "7zip.7zip"
            $packages[0].Override | Should -BeNullOrEmpty
            $packages[0].Uninstall | Should -Be $false

            $packages[1].Id | Should -Be "Microsoft.VisualStudioCode.Insiders"
            $packages[1].Override | Should -Be "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
            $packages[1].Uninstall | Should -Be $false

            $packages[2].Id | Should -Be "Zoom.Zoom"
            $packages[2].Override | Should -BeNullOrEmpty
            $packages[2].Uninstall | Should -Be $true
        }

        It "From Uri" {
            # Arrange: テストの準備

            # Act: 関数を実行
            $packages = Get-GistGetPackage -Uri "https://gist.githubusercontent.com/nuitsjp/82b27147c684502b4f9c4488f9fe6fa9/raw/bd7abee92f8c8f152659960172c96600ae709036/Test-Get-GistGetPackages.yaml"

            # Assert: 結果が期待通りか確認
            $packages.Count | Should -Be 3
            $packages[0].Id | Should -Be "7zip.7zip"
            $packages[0].Override | Should -BeNullOrEmpty
            $packages[0].Uninstall | Should -Be $false

            $packages[1].Id | Should -Be "Microsoft.VisualStudioCode.Insiders"
            $packages[1].Override | Should -Be "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
            $packages[1].Uninstall | Should -Be $false

            $packages[2].Id | Should -Be "Zoom.Zoom"
            $packages[2].Override | Should -BeNullOrEmpty
            $packages[2].Uninstall | Should -Be $true
        }

        It "From GistId" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            $packages = Get-GistGetPackage -GistId "82b27147c684502b4f9c4488f9fe6fa9"
    
            # Assert: 結果が期待通りか確認
            $packages.Count | Should -Be 3
            $packages[0].Id | Should -Be "7zip.7zip"
            $packages[0].Override | Should -BeNullOrEmpty
            $packages[0].Uninstall | Should -Be $false
    
            $packages[1].Id | Should -Be "Microsoft.VisualStudioCode.Insiders"
            $packages[1].Override | Should -Be "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
            $packages[1].Uninstall | Should -Be $false
    
            $packages[2].Id | Should -Be "Zoom.Zoom"
            $packages[2].Override | Should -BeNullOrEmpty
            $packages[2].Uninstall | Should -Be $true
        }

        It "From GistId and FileName" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            $packages = Get-GistGetPackage -GistId "82b27147c684502b4f9c4488f9fe6fa9" -GistFileName "GistGet.yaml"
    
            # Assert: 結果が期待通りか確認
            $packages.Count | Should -Be 3
            $packages[0].Id | Should -Be "7zip.7zip"
            $packages[0].Override | Should -BeNullOrEmpty
            $packages[0].Uninstall | Should -Be $false
    
            $packages[1].Id | Should -Be "Microsoft.VisualStudioCode.Insiders"
            $packages[1].Override | Should -Be "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
            $packages[1].Uninstall | Should -Be $false
    
            $packages[2].Id | Should -Be "Zoom.Zoom"
            $packages[2].Override | Should -BeNullOrEmpty
            $packages[2].Uninstall | Should -Be $true
        }

        It "From GistId environment variable" {
            # Arrange: テストの準備
            Set-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -Value "82b27147c684502b4f9c4488f9fe6fa9"
    
            # Act: 関数を実行
            $packages = Get-GistGetPackage
    
            # Assert: 結果が期待通りか確認
            $packages.Count | Should -Be 3
            $packages[0].Id | Should -Be "7zip.7zip"
            $packages[0].Override | Should -BeNullOrEmpty
            $packages[0].Uninstall | Should -Be $false
    
            $packages[1].Id | Should -Be "Microsoft.VisualStudioCode.Insiders"
            $packages[1].Override | Should -Be "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
            $packages[1].Uninstall | Should -Be $false
    
            $packages[2].Id | Should -Be "Zoom.Zoom"
            $packages[2].Override | Should -BeNullOrEmpty
            $packages[2].Uninstall | Should -Be $true
        }

        It "Ocurred Error" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            { Get-GistGetPackage } | Should -Throw

            # Assert: 結果が期待通りか確認
        }

    }
}
