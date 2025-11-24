# GistGet コントリビューションガイド

GistGetプロジェクトへの貢献に興味を持っていただき、ありがとうございます！このドキュメントでは、プロジェクトへの貢献方法について説明します。

## 目次

- [行動規範](#行動規範)
- [貢献の種類](#貢献の種類)
- [開発環境のセットアップ](#開発環境のセットアップ)
- [貢献のワークフロー](#貢献のワークフロー)
- [プルリクエストのガイドライン](#プルリクエストのガイドライン)
- [コードレビュープロセス](#コードレビュープロセス)
- [リリースプロセス](#リリースプロセス)

## 行動規範

GistGetプロジェクトでは、すべての貢献者に対して敬意を持って接することを期待しています。以下の行動規範を遵守してください:

- 建設的で敬意のあるコミュニケーション
- 他者の意見や経験を尊重
- 建設的な批判を受け入れる姿勢
- コミュニティの利益を最優先

## 貢献の種類

GistGetプロジェクトへの貢献には、以下のような方法があります:

### 🐛 バグ報告

バグを見つけた場合は、[GitHub Issues](https://github.com/nuitsjp/GistGet/issues)で報告してください。

**良いバグ報告には以下が含まれます:**

- 明確で説明的なタイトル
- 再現手順の詳細
- 期待される動作と実際の動作
- スクリーンショットやエラーメッセージ
- 環境情報 (OS、.NETバージョン、wingetバージョン)

**テンプレート:**

```markdown
## 概要
[バグの簡潔な説明]

## 再現手順
1. [最初のステップ]
2. [次のステップ]
3. [...]

## 期待される動作
[何が起こるべきか]

## 実際の動作
[実際に何が起こったか]

## 環境
- OS: Windows 11 23H2
- .NET: 10.0.100
- winget: 1.7.10514
- GistGet: 1.0.0

## 追加情報
[スクリーンショット、ログなど]
```

### ✨ 機能リクエスト

新機能のアイデアがある場合は、[GitHub Issues](https://github.com/nuitsjp/GistGet/issues)で提案してください。

**良い機能リクエストには以下が含まれます:**

- 機能の明確な説明
- ユースケースと動機
- 可能であれば、実装のアイデア
- 代替案の検討

### 📝 ドキュメントの改善

ドキュメントの誤字脱字、不明瞭な説明、不足している情報などを見つけた場合は、プルリクエストを送ってください。

### 💻 コードの貢献

バグ修正や新機能の実装を行う場合は、以下のワークフローに従ってください。

## 開発環境のセットアップ

詳細は[DEVELOPER_GUIDE.ja.md](file:///d:/GistGet/docs/DEVELOPER_GUIDE.ja.md)を参照してください。

### クイックスタート

```powershell
# リポジトリをフォーク
# https://github.com/nuitsjp/GistGet/fork

# フォークしたリポジトリをクローン
git clone https://github.com/YOUR_USERNAME/GistGet.git
cd GistGet

# 依存関係を復元
dotnet restore

# ビルド
dotnet build GistGet.sln -c Debug

# テスト実行
.\scripts\Run-Tests.ps1
```

## 貢献のワークフロー

### 1. Issue の作成または選択

- 新しい機能やバグ修正を始める前に、関連する Issue を作成または選択します
- 既存の Issue に取り組む場合は、コメントで意思表示してください
- 大きな変更の場合は、実装前に Issue で議論することを推奨します

### 2. ブランチの作成

```powershell
# 最新の main ブランチを取得
git checkout main
git pull upstream main

# 新しいブランチを作成
git checkout -b feature/your-feature-name
# または
git checkout -b fix/your-bug-fix-name
```

**ブランチ命名規則:**

- `feature/機能名`: 新機能
- `fix/バグ名`: バグ修正
- `refactor/対象`: リファクタリング
- `docs/対象`: ドキュメント更新
- `test/対象`: テストの追加・修正

### 3. コードの実装

#### TDD (Test-Driven Development) の実践

GistGetプロジェクトでは、**t-wadaスタイルのTDD**を厳格に遵守しています。

**RED-GREEN-REFACTORサイクル:**

1. **RED**: まず失敗するテストを書く
2. **GREEN**: テストを通す最小限の実装を行う
3. **REFACTOR**: コードをリファクタリングして品質を向上させる

```powershell
# テストを書く
# src/GistGet.Tests/Services/YourServiceTests.cs

# テストが失敗することを確認 (RED)
dotnet test

# 実装を追加
# src/GistGet/Application/Services/YourService.cs

# テストが通ることを確認 (GREEN)
dotnet test

# リファクタリング (REFACTOR)
# コードの品質を向上させる

# テストが引き続き通ることを確認
dotnet test
```

#### コーディング規約の遵守

詳細は[DEVELOPER_GUIDE.ja.md](file:///d:/GistGet/docs/DEVELOPER_GUIDE.ja.md#コーディング規約)を参照してください。

**主要なポイント:**

- C# 12、.NET 10.0
- 4スペースインデント
- PascalCase: 型、メソッド、プロパティ
- camelCase: パラメータ、ローカル変数
- `_camelCase`: プライベートreadonly フィールド
- 非同期メソッドには `Async` サフィックス
- インターフェースを使用した依存性注入

### 4. テストの追加

**すべての新機能とバグ修正には、対応するテストが必要です。**

```csharp
// src/GistGet.Tests/Services/YourServiceTests.cs
using Xunit;
using Moq;

public class YourServiceTests
{
    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var mockDependency = new Mock<IDependency>();
        var service = new YourService(mockDependency.Object);
        
        // Act
        var result = await service.MethodAsync();
        
        // Assert
        Assert.NotNull(result);
        mockDependency.Verify(x => x.SomeMethod(), Times.Once);
    }
}
```

**カバレッジ目標:**

- 新規コード: **100%**
- 全体: **95%以上**

```powershell
# カバレッジを確認
.\scripts\Run-Tests.ps1 -CollectCoverage $true
```

### 5. コミット

**Conventional Commits** に従ってコミットメッセージを書いてください。

```
<type>: <subject>

<body>

<footer>
```

**Type:**

- `feat`: 新機能
- `fix`: バグ修正
- `docs`: ドキュメントのみの変更
- `style`: コードの意味に影響しない変更
- `refactor`: バグ修正や機能追加ではないコード変更
- `test`: テストの追加や修正
- `chore`: ビルドプロセスやツールの変更

**例:**

```bash
git add .
git commit -m "feat: add support for package version pinning

- Add Version property to GistGetPackage model
- Implement version comparison in PackageService
- Add unit tests for version pinning logic
- Update YAML_SPEC.ja.md with version syntax

Closes #123"
```

**コミットのベストプラクティス:**

- 1つのコミットには1つの論理的な変更のみを含める
- コミットメッセージは現在形、命令形で書く
- 本文には「何を」「なぜ」変更したかを説明
- 関連する Issue 番号を含める

### 6. プッシュ

```powershell
git push origin feature/your-feature-name
```

### 7. プルリクエストの作成

GitHub上でプルリクエストを作成します。

## プルリクエストのガイドライン

### プルリクエストのタイトル

Conventional Commits形式を使用:

```
feat: add package version pinning support
fix: resolve credential storage issue on Windows 11
docs: update installation instructions
```

### プルリクエストの説明

**テンプレート:**

```markdown
## 概要
[変更の簡潔な説明]

## 変更内容
- [変更点1]
- [変更点2]
- [...]

## 関連 Issue
Closes #123

## テスト
- [ ] 新しいテストを追加した
- [ ] 既存のテストがすべて通る
- [ ] カバレッジが95%以上である

## チェックリスト
- [ ] コードはコーディング規約に従っている
- [ ] TDDサイクルに従って実装した
- [ ] ドキュメントを更新した (必要な場合)
- [ ] コミットメッセージはConventional Commitsに従っている

## スクリーンショット (該当する場合)
[スクリーンショットまたはGIF]

## 追加情報
[その他の関連情報]
```

### プルリクエストのベストプラクティス

1. **小さく保つ**: 1つのPRには1つの機能またはバグ修正のみを含める
2. **テストを含める**: すべての変更に対応するテストを追加
3. **ドキュメントを更新**: 必要に応じてドキュメントを更新
4. **レビュー可能にする**: コードレビューしやすいように、適切なコメントを追加
5. **CI/CDを通す**: すべてのチェックが通ることを確認

### 実行コマンドの記載

PRの説明には、変更を検証するための実行コマンドを含めてください:

```markdown
## 検証方法

### ビルド
```powershell
dotnet build GistGet.sln -c Release
```

### テスト
```powershell
.\scripts\Run-Tests.ps1 -Configuration Release
```

### 動作確認
```powershell
dotnet run --project src/GistGet/GistGet.csproj -- sync
```
```

### スクリーンショットやCLI出力

動作が変更される場合は、スクリーンショットやCLI出力を添付してください:

```markdown
## CLI出力例

```
$ gistget sync
Fetching packages from Gist...
Found 5 packages in Gist
Checking local packages...
Found 3 packages installed locally

Installing 2 new packages:
  ✓ Microsoft.PowerToys v0.75.0
  ✓ Microsoft.VisualStudioCode v1.85.0

Sync completed successfully!
```
```

## コードレビュープロセス

### レビュー観点

レビュアーは以下の観点でコードをレビューします:

1. **機能性**: コードは意図した通りに動作するか
2. **テスト**: 適切なテストが含まれているか
3. **設計**: アーキテクチャとの整合性があるか
4. **可読性**: コードは理解しやすいか
5. **パフォーマンス**: パフォーマンス上の問題はないか
6. **セキュリティ**: セキュリティ上の問題はないか
7. **ドキュメント**: 必要なドキュメントが更新されているか

### レビューへの対応

1. レビューコメントに対して丁寧に対応
2. 変更が必要な場合は、追加のコミットで対応
3. 議論が必要な場合は、コメントで説明
4. すべてのコメントに対応したら、レビュアーに通知

### マージ条件

以下の条件を満たした場合、PRはマージされます:

- [ ] すべてのCIチェックが通る
- [ ] 少なくとも1人のメンテナーによる承認
- [ ] すべてのレビューコメントが解決済み
- [ ] コンフリクトが解消されている
- [ ] カバレッジが95%以上

## リリースプロセス

リリースはメンテナーによって行われます。

### バージョニング

GistGetは[Semantic Versioning](https://semver.org/)に従います:

- **MAJOR**: 互換性のない変更
- **MINOR**: 後方互換性のある機能追加
- **PATCH**: 後方互換性のあるバグ修正

### リリースノート

各リリースには、以下を含むリリースノートが作成されます:

- 新機能
- バグ修正
- 破壊的変更
- 既知の問題

## よくある質問

### Q: どのIssueから始めればいいですか?

A: `good first issue` ラベルが付いたIssueから始めることをお勧めします。これらは比較的簡単で、プロジェクトに慣れるのに適しています。

### Q: 大きな機能を追加したいのですが?

A: まずIssueで提案し、メンテナーと議論してください。設計が承認されてから実装を開始することをお勧めします。

### Q: テストの書き方がわかりません

A: [DEVELOPER_GUIDE.ja.md](file:///d:/GistGet/docs/DEVELOPER_GUIDE.ja.md#テスト戦略)のテスト戦略セクションを参照してください。また、既存のテストコードも参考になります。

### Q: PRがマージされるまでどのくらいかかりますか?

A: PRのサイズと複雑さによりますが、通常は数日以内にレビューが行われます。大きな変更の場合は、より時間がかかる場合があります。

### Q: レビューコメントに同意できない場合は?

A: コメントで丁寧に説明してください。建設的な議論は歓迎します。最終的な判断はメンテナーが行います。

## サポート

質問や問題がある場合は、以下の方法でサポートを受けられます:

- [GitHub Issues](https://github.com/nuitsjp/GistGet/issues): バグ報告、機能リクエスト
- [GitHub Discussions](https://github.com/nuitsjp/GistGet/discussions): 一般的な質問、アイデアの議論

## 謝辞

GistGetプロジェクトへの貢献に感謝します！あなたの貢献がプロジェクトをより良いものにします。

## 参考資料

- [DEVELOPER_GUIDE.ja.md](file:///d:/GistGet/docs/DEVELOPER_GUIDE.ja.md) - 開発者ガイド
- [DESIGN.ja.md](file:///d:/GistGet/docs/DESIGN.ja.md) - システム設計書
- [SPEC.ja.md](file:///d:/GistGet/docs/SPEC.ja.md) - 仕様書
- [YAML_SPEC.ja.md](file:///d:/GistGet/docs/YAML_SPEC.ja.md) - YAML仕様書
