# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリのコードを扱う際のガイダンスを提供します。

## 言語ポリシー

- **思考は英語**で行い、**ユーザーとのやり取りは日本語**で行うこと
- プラン、コミットメッセージ、PRの説明は**日本語**で記述すること

## ビルド・開発コマンド

```powershell
# ビルド
dotnet build src/GistGet.slnx -c Debug

# CLI実行
dotnet run --project src/NuitsJp.GistGet/NuitsJp.GistGet.csproj -- <command>
# 例: -- auth login, -- sync, -- install <id>

# 全テスト実行（カバレッジ付き）
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults

# 特定のテストクラスを実行
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj --filter "FullyQualifiedName~ClassName"

# 特定のテストメソッドを実行
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj --filter "FullyQualifiedName~ClassName.MethodName"

# コード品質パイプライン全体実行（FormatCheck -> Build -> Tests -> ReSharper）
.\scripts\Run-CodeQuality.ps1

# 特定のステップのみ実行
.\scripts\Run-CodeQuality.ps1 -Build           # ビルドのみ
.\scripts\Run-CodeQuality.ps1 -Build -Tests    # ビルドとテストのみ
.\scripts\Run-CodeQuality.ps1 -Tests           # テストのみ

# GitHub認証（統合テスト実行前に必要）
.\scripts\Run-AuthLogin.ps1
```

## アーキテクチャ

GistGetは、GitHub Gistを介してデバイス間でwingetパッケージを同期するCLIツールです。

### プロジェクト構成

```
src/
├── GistGet/                       # ランチャー実行ファイル（薄いラッパー）
│   └── Program.cs                 # NuitsJp.GistGetを呼び出すエントリーポイント
├── NuitsJp.GistGet/               # メインライブラリ
│   ├── Program.cs                 # DIブートストラップとCLIエントリーポイント
│   ├── GistGetService.cs          # メインオーケストレーション（init, sync, installなど）
│   ├── GistGetPackage.cs          # パッケージモデル
│   ├── GistGetPackageSerializer.cs # YAMLシリアライゼーション
│   ├── Presentation/
│   │   ├── CommandBuilder.cs      # 全CLIコマンド定義
│   │   └── ConsoleService.cs      # コンソール出力処理
│   ├── Infrastructure/
│   │   ├── WinGet/                # WinGet COMインターオプヘルパー
│   │   ├── CredentialService.cs   # Windows資格情報マネージャー
│   │   ├── GitHubService.cs       # Gist読み書き操作
│   │   └── WinGetService.cs       # パッケージ検索、インストール、アップグレード、アンインストール
│   └── *Options.cs                # InstallOptions, UpgradeOptions, UninstallOptions
└── NuitsJp.GistGet.Test/          # テストプロジェクト
```

### 主要な依存関係

- **Microsoft.WindowsPackageManager.ComInterop**: WinGet COM API
- **Octokit**: GitHub API（Gist操作）
- **System.CommandLine**: CLI引数パーシング
- **Spectre.Console**: リッチコンソール出力
- **Sharprompt**: `init`コマンド用の対話型プロンプト
- **YamlDotNet**: GistGet.yaml用YAMLシリアライゼーション
- **UnitGenerator**: 値オブジェクト生成（PackageId, Version）

### コアワークフロー

1. **auth login**: GitHubデバイスフロー -> Windows資格情報マネージャーにトークンを保存
2. **init**: ローカルのwingetパッケージ一覧 -> ユーザーが選択 -> GistにGistGet.yamlを作成/更新
3. **sync**: GistGet.yamlを取得 -> ローカルと比較 -> 不足分をインストール、マーク済みをアンインストール
4. **install/upgrade/uninstall**: 標準的なwinget操作 + Gistを更新

## コーディング規約

- **フレームワーク**: .NET 10.0, C# 14, Windows 10.0.26100.0
- **DI**: 全サービスをProgram.csで登録、コンストラクタインジェクション
- **非同期**: 非同期メソッドには`*Async`サフィックスを付与
- **テスト**: xUnit + Moq + Shouldly、厳密なAAAパターン（コメント区切り）
- **TDD**: t-wada流のRED-GREEN-REFACTORサイクルに従う

### テストファイル構成

```csharp
public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();

    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackage_ReturnsPackage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("jqlang.jq");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldNotBeNull();
        }
    }
}
```

## カバレッジ要件

- **行カバレッジ**: 最低98%
- **分岐カバレッジ**: 最低85%
- **ファイル単位の閾値**: 最低89%
