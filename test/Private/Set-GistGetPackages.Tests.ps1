# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-GistGetPackages Tests" {
        BeforeAll {
            # モックの準備
            Mock Set-GistContent { 
            }
        }
    
        It "指定されたGistを更新する" {
            # Arrange: テストの準備

            $gist = [GistFile]::new("FooGistId", "FooGistFileName")
            $packages = @(
                [GistGetPackage]::CreateFromHashtable(
                    @{
                        id = "Microsoft.VisualStudioCode.Insiders"
                        override = "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
                    }
                ),
                [GistGetPackage]::CreateFromHashtable(
                    @{
                        id = "Zoom.Zoom"
                        uninstall = $true
                    }
                ),
                [GistGetPackage]::CreateFromHashtable(
                    @{
                        id = "7zip.7zip"
                    }
                )
            )

            # Act: 関数を実行
            Set-GistGetPackages -Gist $gist -Packages $packages

            # Assert: 結果が期待通りか確認
            $expected = Get-Content -Path "$PSScriptRoot\assets\test.yaml"
            Should -Invoke Set-GistContent -ParameterFilter {
                $Gist -eq $gist -and
                $Content -ne $expected
            }
        }
    }
}
