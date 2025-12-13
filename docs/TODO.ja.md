# TODO（仕様準拠・整合性）

このファイルは、`docs/SPEC.ja.md` と現状実装の差分（仕様不一致/実装漏れ/設計ガイドライン逸脱）を列挙するバックログです。

**最終更新**: 2025年12月13日（winget ヘルプとの比較に基づく追記）

実施したTODOは☑に変更してください。

---

## 🔴 最優先（未実装コマンド）

### export / import コマンド

- [ ] **`export` コマンドが未実装**: コマンド定義のみ、ハンドラなし
  - 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`
- [ ] **`import` コマンドが未実装**: コマンド定義のみ、ハンドラなし
  - 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`
- [ ] `IGistGetService` に `ExportAsync()` / `ImportAsync()` メソッドが存在しない
  - 関連ファイル: `src/GistGet/GistGet/IGistGetService.cs`

---

## 🔴 重大なバグ（データ損失）

### YAML シリアライズ時のフィールド脱落

- [ ] `SerializePackages()` で `pin` / `pinType` が保存されない
  - 原因: コピーオブジェクト作成時に `Pin` / `PinType` が含まれていない
  - 関連ファイル: `src/GistGet/GistGet/Infrastructure/GitHubService.cs` (L179-200)

```csharp
// 現状のコード（問題箇所）
var copy = new GistGetPackage
{
    Version = package.Version,
    Custom = package.Custom,
    // ...
    // Pin, PinType が欠落している！
};
```

- [ ] `SerializePackages()` ですべての属性が正しく保存されているか、全属性についてのユニットテストを追加する

### install の custom オプションが誤っている

- [ ] `--custom` フラグなしで値だけ渡している
  - 現状: `installArgs.Add(options.Custom)` → winget が認識しない
  - 正しくは: `installArgs.Add("--custom"); installArgs.Add(options.Custom);`
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs` (L159)

---

## 🟡 CLI オプション不足

### install コマンド

`InstallOptions` にプロパティはあるが、CLI で受け付けていないオプション:

| オプション | InstallOptions | CLI 定義 | winget 渡し |
|------------|:--------------:|:--------:|:-----------:|
| `--accept-package-agreements` | ✅ | ❌ | ✅ |
| `--accept-source-agreements` | ✅ | ❌ | ✅ |
| `--locale` | ✅ | ❌ | ✅ |
| `--ignore-security-hash` | ✅ (`AllowHashMismatch`) | ❌ | ✅ |

- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (L76-96)

winget に存在するが `InstallOptions` にも CLI にもないオプション:

| オプション | 説明 | 対応要否 |
|------------|------|:--------:|
| `-q,--query` | パッケージの検索クエリ | ❓ |
| `-m,--manifest` | マニフェストパス | ❓ |
| `--name` | 名前でフィルター | ❓ |
| `--moniker` | モニカーでフィルター | ❓ |
| `-s,--source` | ソース指定 | ⚠️ 要検討 |
| `-e,--exact` | 完全一致 | ⚠️ 要検討 |
| `--allow-reboot` | 再起動許可 | ⚠️ 要検討 |
| `--ignore-local-archive-malware-scan` | マルウェアスキャン無視 | ❓ |
| `--dependency-source` | 依存関係ソース | ❓ |
| `--no-upgrade` | 既存時アップグレードスキップ | ⚠️ 要検討 |
| `--authentication-mode` | 認証モード | ❓ |
| `--authentication-account` | 認証アカウント | ❓ |
| `-r,--rename` | 実行ファイル名変更（ポータブル） | ❓ |
| `--uninstall-previous` | アップグレード時に旧バージョン削除 | ⚠️ 要検討 |

- 関連ファイル: `src/GistGet/GistGet/InstallOptions.cs`

### uninstall コマンド

CLI に存在しないオプション（現在 `--id` のみ）:

| オプション | 説明 | 対応要否 |
|------------|------|:--------:|
| `-q,--query` | 検索クエリ | ❓ |
| `-m,--manifest` | マニフェストパス | ❓ |
| `--name` | 名前でフィルター | ❓ |
| `--moniker` | モニカーでフィルター | ❓ |
| `--product-code` | 製品コードでフィルター | ❓ |
| `-v,--version` | バージョン指定 | ⚠️ 要検討 |
| `--all,--all-versions` | 全バージョンアンインストール | ⚠️ 要検討 |
| `-s,--source` | ソース指定 | ⚠️ 要検討 |
| `-e,--exact` | 完全一致 | ⚠️ 要検討 |
| `--scope` | スコープ（user/machine） | ⚠️ 要検討 |
| `-i,--interactive` | 対話モード | ⚠️ 要検討 |
| `-h,--silent` | サイレントモード | ⚠️ 要検討 |
| `--force` | 強制実行 | ⚠️ 要検討 |
| `--purge` | 完全削除（ポータブル） | ⚠️ 要検討 |
| `--preserve` | ファイル保持（ポータブル） | ⚠️ 要検討 |
| `-o,--log` | ログファイルパス | ⚠️ 要検討 |
| `--header` | HTTPヘッダー | ❓ |
| `--authentication-mode` | 認証モード | ❓ |
| `--authentication-account` | 認証アカウント | ❓ |
| `--accept-source-agreements` | ソース契約同意 | ⚠️ 要検討 |

- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (BuildUninstallCommand)

### upgrade コマンド

CLI に存在しないオプション（現在 `--id`, `--version` のみ）:

| オプション | 説明 | 対応要否 |
|------------|------|:--------:|
| `-q,--query` | 検索クエリ | ❓ |
| `-m,--manifest` | マニフェストパス | ❓ |
| `--name` | 名前でフィルター | ❓ |
| `--moniker` | モニカーでフィルター | ❓ |
| `-s,--source` | ソース指定 | ⚠️ 要検討 |
| `-e,--exact` | 完全一致 | ⚠️ 要検討 |
| `-i,--interactive` | 対話モード | ⚠️ 要検討 |
| `-h,--silent` | サイレントモード | ⚠️ 要検討 |
| `--purge` | 完全削除（ポータブル） | ❓ |
| `-o,--log` | ログファイルパス | ⚠️ 要検討 |
| `--custom` | カスタム引数 | ⚠️ 要検討 |
| `--override` | 引数上書き | ⚠️ 要検討 |
| `-l,--location` | インストール先 | ⚠️ 要検討 |
| `--scope` | スコープ（user/machine） | ⚠️ 要検討 |
| `-a,--architecture` | アーキテクチャ | ⚠️ 要検討 |
| `--installer-type` | インストーラタイプ | ⚠️ 要検討 |
| `--locale` | ロケール | ⚠️ 要検討 |
| `--ignore-security-hash` | ハッシュ不一致無視 | ⚠️ 要検討 |
| `--allow-reboot` | 再起動許可 | ⚠️ 要検討 |
| `--skip-dependencies` | 依存関係スキップ | ⚠️ 要検討 |
| `--ignore-local-archive-malware-scan` | マルウェアスキャン無視 | ❓ |
| `--accept-package-agreements` | パッケージ契約同意 | ⚠️ 要検討 |
| `--accept-source-agreements` | ソース契約同意 | ⚠️ 要検討 |
| `--header` | HTTPヘッダー | ❓ |
| `--authentication-mode` | 認証モード | ❓ |
| `--authentication-account` | 認証アカウント | ❓ |
| `-r,--recurse,--all` | 全パッケージアップグレード | ✅ パススルー対応済 |
| `-u,--unknown,--include-unknown` | バージョン不明も含める | ⚠️ 要検討 |
| `--pinned,--include-pinned` | ピン済みも含める | ⚠️ 要検討 |
| `--uninstall-previous` | 旧バージョン削除 | ⚠️ 要検討 |
| `--force` | 強制実行 | ⚠️ 要検討 |

- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (BuildUpgradeCommand)

### pin add コマンド

CLI に存在しないオプション:

| オプション | 説明 | 対応要否 |
|------------|------|:--------:|
| `--blocking` | ブロッキング pin | ⚠️ 要実装 |
| `--gating` | ゲーティング pin | ⚠️ 要実装 |
| `--installed` | インストール済みバージョンに固定 | ⚠️ 要検討 |
| `-s,--source` | ソース指定 | ❓ |
| `-e,--exact` | 完全一致 | ❓ |
| `--force` | 強制上書き | ⚠️ 要検討（GistGetServiceで追加済み） |

- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (BuildPinCommand)

### pin remove コマンド

CLI に存在しないオプション:

| オプション | 説明 | 対応要否 |
|------------|------|:--------:|
| `-q,--query` | 検索クエリ | ❓ |
| `--name` | 名前でフィルター | ❓ |
| `--moniker` | モニカーでフィルター | ❓ |
| `-s,--source` | ソース指定 | ❓ |
| `--tag` | タグでフィルター | ❓ |
| `--cmd,--command` | コマンドでフィルター | ❓ |
| `-e,--exact` | 完全一致 | ❓ |
| `--header` | HTTPヘッダー | ❓ |
| `--authentication-mode` | 認証モード | ❓ |
| `--authentication-account` | 認証アカウント | ❓ |
| `--accept-source-agreements` | ソース契約同意 | ❓ |
| `--installed` | インストール済みから解除 | ❓ |

- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (BuildPinCommand)

### winget パススルーコマンドの不足

winget v1.12 で利用可能なコマンド一覧と GistGet の対応状況:

| winget コマンド | GistGet 対応 | 備考 |
|----------------|:------------:|------|
| `install` | ✅ | 管理対象コマンド |
| `show` | ✅ | パススルー |
| `source` | ✅ | パススルー |
| `search` | ✅ | パススルー |
| `list` | ✅ | パススルー |
| `upgrade` | ✅ | 管理対象コマンド |
| `uninstall` | ✅ | 管理対象コマンド |
| `hash` | ✅ | パススルー |
| `validate` | ✅ | パススルー |
| `settings` | ✅ | パススルー |
| `features` | ✅ | パススルー |
| `export` | ⚠️ | 定義のみ、ハンドラなし |
| `import` | ⚠️ | 定義のみ、ハンドラなし |
| `pin` | ✅ | 管理対象コマンド |
| `configure` | ✅ | パススルー |
| `download` | ✅ | パススルー |
| `repair` | ✅ | パススルー |
| `dscv3` | ❌ | 未実装（要検討） |
| `mcp` | ❌ | 未実装（要検討） |

- 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (BuildWingetPassthroughCommands)

---

## 🟡 YAML スキーマの不整合

### GistGetPackage に仕様外のプロパティが存在

仕様書に定義されていないプロパティ（削除候補）:

- [ ] `Mode` プロパティ
- [ ] `Confirm` プロパティ
- [ ] `WhatIf` プロパティ

- 関連ファイル: `src/GistGet/GistGet/GistGetPackage.cs`

### acceptPackageAgreements / acceptSourceAgreements の保存

- [ ] `GistGetPackage` にプロパティはあるが、`SerializePackages()` でコピーされていない
  - 関連ファイル: `src/GistGet/GistGet/Infrastructure/GitHubService.cs`

---

## 🟡 エラーハンドリング不足

### winget 失敗時のプロセス終了コード

- [ ] `InstallAndSaveAsync` / `UninstallAndSaveAsync` / `UpgradeAndSaveAsync` が winget 失敗時に `return` するだけで、呼び出し元に失敗を伝達しない
  - 現状: Gist を更新しないが、CLI としては正常終了
  - 期待: 非ゼロ終了コードを返す
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`

### パススルー引数のクォート/エスケープ

- [ ] `string.Join(" ", args)` でスペースを含む引数が壊れる可能性
  - 関連ファイル: `src/GistGet/GistGet/Infrastructure/WinGetPassthroughRunner.cs` (L12)

---

## 🟡 upgrade コマンドの問題

### pin 追従時のバージョン取得

- [ ] upgrade 成功後の pin 追従で「更新可能バージョン（UsableVersion）」を使用しているが、upgrade 後のインストール済みバージョンを取得すべき
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs` (L256-258)
  - 関連ファイル: `src/GistGet/GistGet/Infrastructure/WinGetService.cs`
- [ ] Gist に pin が無くローカルに pin がある場合、Gist を正として上書きせず pin 同期も Gist 更新も行わないため、明示 upgrade 後に Gist 側へ pin を記録する処理が必要
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs`

### ID 未指定時のパススルー引数再構成

- [ ] `ParseResult.Tokens` 依存で引数再構成が不安定
  - 関連ファイル: `src/GistGet/GistGet/Presentation/CommandBuilder.cs` (L161-187)

---

## 🟡 uninstall コマンドの問題

### ローカル pin の残存

- [ ] Gist 側の `pin` 有無でしか `pin remove` を判断していない
  - ローカルに pin があるが Gist にエントリがない場合、pin が残る
  - 関連ファイル: `src/GistGet/GistGet/GistGetService.cs` (L209-212)

---

## 🟢 正しく実装されている機能

| 機能 | 状態 | 備考 |
|------|:----:|------|
| `auth login` | ✅ | Device Flow 認証 |
| `auth logout` | ✅ | 資格情報削除 |
| `auth status` | ✅ | トークン状態表示 |
| `install` | ⚠️ | 動作するが CLI オプション不足・custom バグあり |
| `uninstall` | ⚠️ | 動作するがローカル pin 残存問題あり |
| `upgrade` (ID 指定時) | ⚠️ | 動作するがバージョン取得問題あり |
| `upgrade` (ID 未指定) | ✅ | パススルー |
| `pin add` | ⚠️ | 動作するが YAML 保存で pin 脱落 |
| `pin remove` | ⚠️ | 動作するが YAML 保存で問題あり |
| `pin list` / `pin reset` | ✅ | パススルー |
| winget パススルー (11 コマンド) | ✅ | list, search, show 等 |

---

## 📋 csproj / 依存関係

- [ ] `TargetFramework` を `net10.0-windows10.0.26100.0` に更新する
  - 関連ファイル: `src/GistGet/GistGet.csproj`
- [ ] `Microsoft.Identity.Client` を削除する（Octokit で認証しており不要）
  - 関連ファイル: `src/GistGet/GistGet.csproj`

---

## 📋 Gist ファイル名の揺れ

- [ ] `GitHubService` のデフォルトファイル名が `gistget-packages.yaml`
- [ ] `GistGetService` の呼び出しは `packages.yaml` を渡している
- [ ] 仕様書は `packages.yaml` と記載
- 動作に問題はないが、デフォルト値の統一が必要
  - 関連ファイル: `src/GistGet/GistGet/Infrastructure/GitHubService.cs` (L9)

---

## 📋 仕様書（SPEC）自体の不整合

- [ ] `sync` 節で `gistget upgrade --id <id> --pin <version>` と記載があるが、`upgrade` 節は `--version` を定義している（`--pin` オプションは未定義）
  - 関連ファイル: `docs/SPEC.ja.md`

---

## 📋 テスト追加が必要な項目

- [ ] sync の同期マトリクス（実装後）
- [ ] export / import の動作（実装後）
- [ ] YAML シリアライズで全フィールドが保存されること
- [ ] winget 失敗時のエラー伝播
- [ ] custom オプションの正しいパススルー
