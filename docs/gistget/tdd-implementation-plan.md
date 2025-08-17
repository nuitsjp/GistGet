# t-wada式TDD実装修正計画

## 概要
YAML構造の修正に伴い、t-wada式TDDアプローチで.NET実装を修正する計画です。PowerShellモジュール版の実装を参考に、正確なYAML辞書形式での実装を行います。

## t-wada TDDアプローチ

### 基本サイクル
1. **Red**: まず失敗するテストを書く
2. **Green**: テストを通す最小限のコードを書く  
3. **Refactor**: コードをリファクタリングする

### 適用方針
- 各Issue（機能）ごとに小さなサイクルを回す
- 一度に一つの責務のみに集中
- 実装前に必ずテストを書く

## 修正が必要な実装領域

### 1. データモデル層の修正

#### Issue 1: PackageDefinitionクラスの修正
**現在の問題**: PowerShell版とのデータ構造不一致

**TDDサイクル1**:
- **Red**: PowerShell版Classes.ps1と同等のプロパティを持つPackageDefinitionクラステスト
- **Green**: PowerShell版準拠のプロパティ実装
- **Refactor**: プロパティ名の統一とnull許可設定

**期待されるクラス構造**:
```csharp
public class PackageDefinition
{
    public string Id { get; set; }
    public bool? AllowHashMismatch { get; set; }
    public string Architecture { get; set; }
    public string Custom { get; set; }
    public bool? Force { get; set; }
    public string Header { get; set; }
    public string InstallerType { get; set; }
    public string Locale { get; set; }
    public string Location { get; set; }
    public string Log { get; set; }
    public string Mode { get; set; }
    public string Override { get; set; }
    public string Scope { get; set; }
    public bool? SkipDependencies { get; set; }
    public string Version { get; set; }
    public bool? Confirm { get; set; }
    public bool? WhatIf { get; set; }
    public bool? Uninstall { get; set; }
}
```

#### Issue 2: YAML Serialization/Deserializationの修正
**現在の問題**: Packagesコレクション形式での実装

**TDDサイクル2**:
- **Red**: 辞書形式のYAMLシリアライゼーションテスト
- **Green**: Dictionary<string, PackageDefinition>でのYAML処理実装
- **Refactor**: YamlDotNetの設定最適化

**期待されるYAML構造**:
```yaml
Git.Git:
  version: 2.43.0
Microsoft.VisualStudioCode:
Microsoft.PowerToys:
  scope: user
  uninstall: true
```

### 2. Business Layer（サービス層）の修正

#### Issue 3: IGistManagerの修正
**現在の問題**: PackageCollection型での処理

**TDDサイクル3**:
- **Red**: Dictionary<string, PackageDefinition>を返すGetGistContentAsyncテスト
- **Green**: 辞書形式でのGist取得・更新実装
- **Refactor**: GitHubAPIクライアントとの結合部最適化

**修正対象メソッド**:
```csharp
public interface IGistManager
{
    Task<Dictionary<string, PackageDefinition>> GetGistContentAsync();
    Task<bool> UpdateGistContentAsync(Dictionary<string, PackageDefinition> packages);
    Task<bool> AddPackageDefinitionAsync(string packageId, PackageDefinition definition);
    Task<bool> RemovePackageDefinitionAsync(string packageId);
    Task<bool> UpdatePackageDefinitionAsync(string packageId, PackageDefinition definition);
}
```

#### Issue 4: GistSyncServiceの修正
**現在の問題**: PackageCollectionベースの差分検出ロジック

**TDDサイクル4**:
- **Red**: 辞書形式での差分検出テスト（install/uninstall/sync各シナリオ）
- **Green**: Dictionary<string, PackageDefinition>ベースの差分検出実装
- **Refactor**: 共通ロジックの抽出とパフォーマンス最適化

**修正対象ロジック**:
```csharp
public class GistSyncService
{
    private SyncPlan DetectDifferences(
        Dictionary<string, PackageDefinition> gistPackages,
        List<PackageDefinition> installedPackages)
    {
        // 辞書キーベースの差分検出
        // uninstall: trueの処理
        // バージョン比較ロジック
    }
}
```

### 3. Infrastructure Layer（データアクセス層）の修正

#### Issue 5: YamlParserの修正
**現在の問題**: コレクション形式のYAML解析

**TDDサイクル5**:
- **Red**: PowerShell版test.yamlと同等のYAML解析テスト
- **Green**: 辞書形式YAML解析の実装
- **Refactor**: エラーハンドリングとパフォーマンス最適化

**テストケース**:
```csharp
[Fact]
public void ParseYaml_ShouldHandleDictionaryFormat()
{
    var yaml = @"
Git.Git:
Microsoft.VisualStudioCode:
  version: 1.85.0
  scope: user
Zoom.Zoom:
  uninstall: true
";
    var result = _yamlParser.ParsePackageDefinitions(yaml);
    
    result.Should().HaveCount(3);
    result["Git.Git"].Should().NotBeNull();
    result["Microsoft.VisualStudioCode"].Version.Should().Be("1.85.0");
    result["Zoom.Zoom"].Uninstall.Should().BeTrue();
}
```

#### Issue 6: GitHubGistClientの修正
**現在の問題**: JSON/YAML変換処理の不整合

**TDDサイクル6**:
- **Red**: GitHub API結果と辞書形式変換のテスト
- **Green**: 正確なGist更新実装
- **Refactor**: HTTPクライアント処理の最適化

### 4. Presentation Layer（コマンド層）の修正

#### Issue 7: InstallCommandの修正
**TDDサイクル7**:
- **Red**: install実行後のYAML更新テスト（辞書形式）
- **Green**: 辞書形式でのパッケージ追加実装
- **Refactor**: コマンドオプション処理の統一

#### Issue 8: UninstallCommandの修正
**TDDサイクル8**:
- **Red**: uninstall実行後のキー削除テスト
- **Green**: 辞書からのキー削除実装
- **Refactor**: エラーハンドリングの統一

#### Issue 9: UpgradeCommandの修正
**TDDサイクル9**:
- **Red**: upgrade実行後のバージョン更新テスト
- **Green**: 辞書内パッケージ定義更新実装
- **Refactor**: バージョン管理ロジックの最適化

#### Issue 10: SyncCommandの修正
**TDDサイクル10**:
- **Red**: uninstall: trueフラグ処理テスト
- **Green**: 辞書ベース同期ロジック実装
- **Refactor**: 同期アルゴリズムの最適化

## 実装スケジュール

### Phase 1: Foundation（週1）
- Issue 1: PackageDefinitionクラス修正
- Issue 2: YAML Serialization修正
- Issue 5: YamlParser修正

### Phase 2: Core Services（週2）
- Issue 3: IGistManager修正
- Issue 6: GitHubGistClient修正
- Issue 4: GistSyncService修正

### Phase 3: Commands（週3）
- Issue 7: InstallCommand修正
- Issue 8: UninstallCommand修正
- Issue 9: UpgradeCommand修正
- Issue 10: SyncCommand修正

## テスト戦略

### 単体テスト
```csharp
// 例: PackageDefinition YAML処理テスト
public class PackageDefinitionYamlTests
{
    [Theory]
    [InlineData("Git.Git:", "Git.Git", null)]
    [InlineData("Microsoft.VisualStudioCode:\n  version: 1.85.0", "Microsoft.VisualStudioCode", "1.85.0")]
    public void ParseYaml_ShouldHandleVariousFormats(string yaml, string expectedId, string expectedVersion)
    {
        // Red -> Green -> Refactor サイクル
    }
}
```

### 統合テスト
```csharp
// 例: 実際のPowerShell版YAMLファイルとの互換性テスト
public class PowerShellCompatibilityTests
{
    [Fact]
    public async Task ShouldParseActualPowerShellYaml()
    {
        var powershellYaml = await File.ReadAllTextAsync("powershell/test/Public/assets/test.yaml");
        var packages = _yamlParser.ParsePackageDefinitions(powershellYaml);
        
        // PowerShell版と同じ結果を期待
        packages.Should().NotBeEmpty();
    }
}
```

### E2Eテスト
```csharp
// 例: 実際のGistとの同期テスト
public class GistSyncE2ETests
{
    [Fact]
    public async Task ShouldSyncWithActualGist()
    {
        // 実際のテスト用Gistを使用した完全な同期テスト
    }
}
```

## 品質保証

### コードカバレッジ目標
- 単体テスト: 90%以上
- 統合テスト: 主要シナリオ100%
- E2Eテスト: 全コマンド動作確認

### CI/CD統合
```yaml
# GitHub Actions例
name: TDD Implementation
on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Run Red Tests
        run: dotnet test --filter "Category=Red"
        continue-on-error: true
      
      - name: Run All Tests
        run: dotnet test --collect:"XPlat Code Coverage"
      
      - name: PowerShell Compatibility Test
        run: |
          # PowerShell版テストYAMLとの比較
          dotnet test --filter "Category=PowerShellCompatibility"
```

## リスク管理

### 技術リスク
- **YAML解析ライブラリの制限**: YamlDotNetの設定で解決
- **PowerShell版との非互換**: 詳細なテストケースで早期発見
- **パフォーマンス劣化**: ベンチマークテストで監視

### 対策
- 各Issueで必ずPowerShell版との比較テスト実施
- CI/CDでの自動回帰テスト
- 段階的なリリースによる問題の早期発見

## 成功指標

### 機能要件
- PowerShell版test.yamlの100%互換解析
- 既存Gistとの完全な同期動作
- 全コマンドでの正確なYAML更新

### 品質要件
- テストカバレッジ90%以上維持
- CI/CDでの全テスト通過
- メモリ使用量の最適化

この計画に基づき、t-wada式TDDアプローチで段階的かつ確実に実装を修正し、PowerShell版と完全に互換性のある.NET版GistGetを構築します。