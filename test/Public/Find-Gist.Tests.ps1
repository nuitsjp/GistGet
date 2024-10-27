# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Find-Gist exist environment variables Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-UserEnvironmentVariable { 
                param($Name)
                if ($Name -eq "GIST_GET_GIST_ID") {
                    return 'Foo'
                }
                elseif ($Name -eq "GIST_GET_GIST_FILE_NAME") {
                    return 'Bar'
                }

                throw
            }
        }

        It "環境変数が存在する場合、正しく取得されること" {
            # Arrange: テストの準備

            # Act: 関数を実行
            $result = Find-Gist

            # Assert: 結果が期待通りか確認
            $result | Should -Not -Be $null
            $result.Id | Should -Be 'Foo'
            $result.FileName | Should -Be 'Bar'
        }
    }

    Describe "Get-GistGetGistId fail Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'FooBar'
            }
        }

        It "値が設定されていない場合、値が取得されないこと" {
            # Arrange: テストの準備
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }

            # Act: 関数を実行
            $result = Get-GistGetGistId

            # Assert: 結果が期待通りか確認
            $result | Should -Be $null
            if(-not $result) {
                $true | Should -Be $true
            }
            if($result) {
                $false | Should -Be $true
            }
        }

        AfterEach {
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }
        }
    }

    Describe "Get-GistGetGistId Multiple fail Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'GistGet Multiple'
            }
        }

        It "値が設定されていない場合、値が取得されないこと" {
            # Arrange: テストの準備
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }

            # Act: 関数を実行
            { Get-GistGetPackage } | Should -Throw
        }

        AfterEach {
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }
        }
    }

}