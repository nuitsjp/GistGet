# GistGet .NET 8 実装ロードマップ

## 概要
winget.exe完全準拠の.NET 8アプリケーション開発のロードマップ。t-wada式TDDに従い、各コマンドを全レイヤーで着実に実装します。

---

## 🎯 現在の進捗サマリー
- ✅ 完了
  - フェーズ1: WinGetコマンド完全仕様書
  - フェーズ2: カスタム引数パーサー（18コマンド完全実装）
  - フェーズ3: COM API基本統合
  - フェーズ3.5: アーキテクチャ簡素化（CLIフォールバック削除）
  - フェーズ4: COM APIラッパー基本実装（WinGetComClient）
- 🚨 進行中（最優先）
  - フェーズ5: コマンド別TDD実装
- ⏳ 未着手
  - フェーズ6: Gist同期機能統合
  - フェーズ7: プロダクション品質保証

---

## 📋 フェーズ5: コマンド別TDD実装（現在）

### 🔴 共通実装ルール

#### 1. 実装前のwinget動作確認
各コマンド実装前に、実際のwingetで全パラメーターの動作を確認：

```powershell
# コマンドヘルプで全オプション確認
winget [command] -?
# 各パラメーターの実動作確認と出力記録
```

#### 2. t-wada式TDDサイクル
各機能の実装は以下の小さなサイクルで実施：

```
【Red-Green-Refactorサイクル】
1. Red（赤）: 失敗するテストを書く
   - 期待する動作を明確にテストコードで表現
   - テストが失敗することを確認

2. Green（緑）: テストを通す最小実装
   - テストを通すための最小限のコードのみ記述
   - 過度な設計や最適化は行わない

3. Refactor（リファクタ）: コード整理
   - テストが通る状態を維持しながらコードを改善
   - 重複の除去、命名の改善、構造の整理
```

#### 3. レイヤー別実装順序
1. **ArgumentParser**: 引数解析とバリデーション
2. **CommandHandler**: ビジネスロジックとフロー制御
3. **WinGetClient**: COM API呼び出し
4. **Integration**: レイヤー間の結合
5. **E2E**: エンドツーエンド動作確認

#### 4. エイリアステスト戦略
```csharp
// 基底テストクラスでエイリアスをパラメータ化
[Theory]
[InlineData("list")]
[InlineData("ls")]
public async Task Command_WithAlias_ExecutesSameLogic(string commandAlias)
{
    // 共通のテストロジック
}
```

---

## 📦 Command 1: list / ls コマンド完全実装

### 🎯 list コマンド仕様（実際のwinget list -?から取得）
```
使用法: winget list [[-q] <query>] [<options>]

引数:
  -q,--query                    リスト コマンドのフィルターとして使用されるクエリ

オプション:
  --id                          結果をパッケージ ID でフィルター処理します
  --name                        結果をパッケージ名でフィルター処理します
  --moniker                     結果をパッケージ モニカーでフィルター処理します
  --tag                         結果をタグでフィルター処理します
  --cmd,--command               結果をコマンドでフィルター処理します
  -s,--source                   指定されたソースを使用してパッケージを検索します
  -e,--exact                    完全一致を使用してパッケージを検索します
  --scope                       使用するインストール スコープを選択します（user または machine）
  -n,--count                    指定した数になるまで出力される項目数を制限します
  --upgrade-available           アップグレードが利用可能なパッケージのみ表示します
  -u,--unknown,--include-unknown アップグレード可能なリストに不明なバージョンを含めます
  --pinned,--include-pinned     アップグレード可能なリストにピン留めされたパッケージを含めます
  --header                      オプションの Windows-Package-Manager REST ソース HTTP ヘッダー
  --authentication-mode         REST ソースの認証モードを指定します
  --authentication-account      REST ソースの認証に使用するアカウントを指定します
  --accept-source-agreements    ソース使用許諾契約に同意し、プロンプトを回避します
  -?,--help                     このコマンドのヘルプを表示します
  --wait                        任意のキーが押されるまでプロセスを終了する前に、プロンプトを表示します
  --logs,--open-logs            既定のログの場所を開きます
  --verbose,--verbose-logs      ログで詳細ログを有効にします（サポートを求める際に役立ちます）
  --nowarn,--ignore-warnings    警告メッセージを表示しません
  --disable-interactivity       対話形式のプロンプトを無効にします
  --proxy                       ネットワーク プロキシを設定します
  --no-proxy                    ネットワーク プロキシの使用を無効にします
```

**重要な動作仕様（実際のwinget動作から確認）**:
- `--include-unknown`と`--include-pinned`は`--upgrade-available`がない場合エラー
- `-n,--count`の範囲は1-1000（範囲外はエラー）
- `--scope`の値は`user`または`machine`のみ（大文字小文字区別なし）
- 複数のフィルターは AND 条件で結合される

### 📐 Layer 1: ArgumentParser (WinGetArgumentParser)

- [ ] 基本引数解析（引数なし、-q/--query）
- [ ] フィルタオプション解析（--id, --name, --moniker, --tag, --cmd）
- [ ] 追加オプション解析（-s, -e, --scope, -n）
- [ ] アップグレード関連解析（--upgrade-available と依存オプション）
- [ ] 認証オプション解析（--header, --authentication-*）
- [ ] エイリアス動作確認（list vs ls）

### 📐 Layer 2: CommandHandler (ListCommandHandler)

- [ ] 基本実行フロー（IWinGetClient呼び出し）
- [ ] フィルタ適用ロジック（複数フィルタの結合）
- [ ] 結果表示フォーマット（テーブル形式、件数制限）
- [ ] エラーハンドリング（例外処理、終了コード）

### 📐 Layer 3: WinGetClient (WinGetComClient)

- [ ] COM API初期化と接続
- [ ] 基本リスト取得（全パッケージ、空リスト）
- [ ] フィルタリング実装（ID、名前、完全一致）
- [ ] ソースフィルタリング
- [ ] アップグレード検出
- [ ] パフォーマンス最適化（ページング、キャンセレーション）

### 📐 Layer 4: Integration Tests

- [ ] 実COM API結合テスト（SkippableFact使用）
- [ ] winget.exe連携テスト（結果比較）

### 📐 Layer 5: E2E Tests

#### 実用的なE2Eテスト戦略
```
【組み合わせ爆発対策】
1. 代表的なパラメーター組み合わせのみテスト
2. 境界値テストに重点
3. 実際のユーザーシナリオベース
4. 条件付き実行（SkippableFact）で実行時間制御
```

- [ ] 基本シナリオテスト
  - [ ] `E2E_List_NoArguments_ShowsAllPackages`
  - [ ] `E2E_List_WithQuery_FiltersCorrectly`
  - [ ] `E2E_List_WithIdExact_FindsSpecificPackage`
- [ ] 重要な組み合わせテスト（5-10パターン）
  - [ ] `E2E_List_QueryAndSource_CombinesFilters`
  - [ ] `E2E_List_UpgradeAvailableWithUnknown_ShowsAll`
  - [ ] `E2E_List_CountLimit_LimitsResults`
- [ ] エイリアステスト
  - [ ] `E2E_ListVsLs_ProduceSameOutput`
- [ ] エラーシナリオテスト
  - [ ] `E2E_List_InvalidCount_ShowsError`
  - [ ] `E2E_List_ConflictingOptions_ShowsError`

#### テスト組み合わせ選定基準
```
【高優先度】
- 最も使用頻度の高い組み合わせ
- エラーが発生しやすい境界値
- 相互依存関係のあるオプション

【除外対象】
- 機能的に重複する組み合わせ
- ArgumentParserで検証済みの組み合わせ
- 実行時間が5秒以上かかる組み合わせ
```

#### パフォーマンス制御
```csharp
[SkippableFact]
[Trait("Category", "E2E")]
[Trait("ExecutionTime", "Fast")]  // 3秒以内
public async Task E2E_List_BasicScenarios_FastExecution()
{
    Skip.IfNot(IsWinGetAvailable() && IsFastTestEnabled(), "高速テストモードのみ");
    // 代表的なシナリオのみ
}

[SkippableFact]
[Trait("Category", "E2E")]
[Trait("ExecutionTime", "Slow")]  // 10秒以内
public async Task E2E_List_ComplexScenarios_DetailedValidation()
{
    Skip.IfNot(IsWinGetAvailable() && IsSlowTestEnabled(), "詳細テストモード");
    // 複雑な組み合わせテスト
}
```
