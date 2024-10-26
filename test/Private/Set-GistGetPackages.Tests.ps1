# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-GistGetPackages Tests" {
        BeforeAll {
            # モックの準備
            Mock Set-Gist { 
            }
        }
    
        It "Path名が指定された場合、指定されたファイルを更新する" {
            # Arrange: テストの準備

            # tempファイルを作成
            $tempFile = [System.IO.Path]::GetTempFileName()
            Remove-Item -Path $tempFile -Force

            # GistGitPackages配列を作成
            $packages = @(
                [PSCustomObject]@{
                    id = "Microsoft.VisualStudioCode.Insiders"
                    packageParameters = "/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath"
                },
                [PSCustomObject]@{
                    id = "Zoom.Zoom"
                    uninstall = $true
                },
                [PSCustomObject]@{
                    id = "7zip.7zip"
                }
            )

            # Act: 関数を実行
            Set-GistGetPackages -Path $tempFile -Packages $packages

            # Assert: 結果が期待通りか確認
            $result = Get-Content -Path $tempFile
            $expected = Get-Content -Path "$PSScriptRoot\assets\test.yaml"
            $result | Should -Be $expected
        }

        It "GistIdが指定された場合、指定されたGistを更新する" {
            # Arrange: テストの準備

            # GistGitPackages配列を作成
            $packages = @(
                [GistGetPackage]::new("7zip.7zip", "", $false)
            )

            # Act: 関数を実行
            Set-GistGetPackages -GistId "FooGistId" -GistFileName "FooGistFileName" -Packages $packages

            # Assert: 結果が期待通りか確認
            Should -Invoke Set-Gist -ParameterFilter {
                $GistId -eq "FooGistId"
                $GistFileName -eq "FooGistFileName"
            }
        }
    }
}
