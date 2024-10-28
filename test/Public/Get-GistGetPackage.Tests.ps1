# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

$content = Get-Content -Path "$PSScriptRoot\assets\test.yaml" -Raw

InModuleScope GistGet {
    Describe "Get-GistGetPackage Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistFile {
                return [GistFile]::new("82b27147c684502b4f9c4488f9fe6fa9", "GistGet.yaml")
            }
            Mock Get-GistDescription { 
                return 'FooBar'
            }

            Mock Get-GitHubGist {
                $content = "7zip.7zip: {}" + [System.Environment]::NewLine
                $content += "Microsoft.VisualStudioCode.Insiders:" + [System.Environment]::NewLine
                $content += "  override: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath" + [System.Environment]::NewLine
                $content += "Zoom.Zoom:" + [System.Environment]::NewLine
                $content += "  uninstall: true" + [System.Environment]::NewLine
                $result = @(
                    [PSCustomObject]@{
                        Description = "GistGet Test"
                        id = "82b27147c684502b4f9c4488f9fe6fa9"
                        files = [PSCustomObject]@{
                            'GistGet.yaml' = @{
                                content = $content
                            }
                        }
                    }
                )
                return $result
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

        It "From Gist" {
            # Arrange: テストの準備
    
            # Act: 関数を実行
            $gist = [GistFile]::new("82b27147c684502b4f9c4488f9fe6fa9", "GistGet.yaml")
            $packages = Get-GistGetPackage -Gist $gist
    
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

        It "From Environment" {
            # Arrange: テストの準備
    
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
    }
}
