# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Get-GistGetGistId Tests" {
        BeforeAll {
            # モックの準備
            Mock Get-GistDescription { 
                return 'GistGet'
            }
        }

        It "値が設定されている場合、正しく取得されること" {
            # Arrange: テストの準備
            $currentDateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            Set-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -Value $currentDateTime

            # Act: 関数を実行
            $result = Get-GistGetGistId

            # Assert: 結果が期待通りか確認
            $result | Should -Be $currentDateTime
        }

        It "値が設定されていない場合、Gist descriptionから取得されること" {
            # Arrange: テストの準備
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }

            # Act: 関数を実行
            $result = Get-GistGetGistId

            # Assert: 結果が期待通りか確認
            $result | Should -Be '49990de4389f126d1f6d57c10c408a0c'
        }

        AfterEach {
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }
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