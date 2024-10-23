# テスト対象のモジュールをインポート
Import-Module -Name "$PSScriptRoot\..\..\src\GistGet.psd1" -Force

InModuleScope GistGet {
    Describe "Set-GistGetGistId Tests" {
        It "値が正しく設定されること" {
            # Arrange: テストの準備
            $currentDateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            Set-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -Value $currentDateTime

            # Act: 関数を実行
            Set-GistGetGistId -GistId $currentDateTime

            # Assert: 結果が期待通りか確認
            $result = [System.Environment]::GetEnvironmentVariable($global:GistGetGistId, [System.EnvironmentVariableTarget]::User)
            $result | Should -Be $currentDateTime
        }

        It "Set-GistGetGistIdの結果がGet-GistGetGistIdで取得できること" {
            # Arrange: テストの準備
            $currentDateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            Set-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -Value $currentDateTime

            # Act: 関数を実行
            Set-GistGetGistId -GistId $currentDateTime

            # Assert: 結果が期待通りか確認
            $result = Get-GistGetGistId
            $result | Should -Be $currentDateTime
        }

        AfterEach {
            if (Get-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId -ErrorAction SilentlyContinue) {
                Remove-ItemProperty -Path "HKCU:\Environment" -Name $Global:GistGetGistId
            }
        }
    }
}
