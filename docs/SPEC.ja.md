# GistGet 仕様書

GistGet は、Windows Package Manager (winget) のパッケージ管理状態を GitHub Gist を介してクラウド同期するツールです。

> **対応 winget バージョン**: v1.12.420 以降  
> **最終更新**: 2025年12月

## 設計原則

1. **winget の透過的なラッパー**: winget のオプションを極力そのままパススルーする
2. **Gist を単一情報源 (Single Source of Truth)**: `packages.yaml` がすべてのデバイスで共有される正規の状態
3. **明示的な ID 指定**: 曖昧な `--query` や `--name` は禁止し、`--id` による一意な指定を必須とする
4. **シンプルなバージョン管理**: pinする場合のみバージョンを管理し、それ以外は常に最新版を使用

---

## YAML スキーマ仕様

### 概要

`packages.yaml` は、パッケージ ID をキーとし、インストールオプションと同期フラグを値とするマップです。

```yaml
<PackageId>:
  pin: <string>                   # ピン留めバージョン（省略でピン留めなし）
  pinType: <pinning | blocking | gating>  # ピンの種類（省略時はpinning）
  uninstall: <boolean>            # trueでアンインストール対象
  # インストールオプション（winget パススルー）
  scope: <user | machine>
  architecture: <x86 | x64 | arm | arm64>
  installerType: <string>
  interactive: <boolean>
  silent: <boolean>
  locale: <string>
  location: <string>
  log: <string>
  custom: <string>
  override: <string>
  force: <boolean>
  acceptPackageAgreements: <boolean>
  acceptSourceAgreements: <boolean>
  allowHashMismatch: <boolean>
  skipDependencies: <boolean>
  header: <string>
```

### パラメータ詳細

#### コアパラメータ

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `pin` | string | ピン留めするバージョン。省略でピン留めなし（常に最新版）。ワイルドカード `*` 使用可（例: `1.7.*`）。 |
| `pinType` | enum | ピンの種類。`pin` が指定されている場合のみ有効。省略時は `pinning`。 |
| `uninstall` | boolean | `true` の場合、sync 時にアンインストールされる。 |

#### pinType の種類

| 値 | 説明 | `upgrade --all` | `upgrade <pkg>` | `upgrade --version <v>` |
|----|------|-----------------|-----------------|-------------------------|
| なし | pin なし。すべての upgrade 対象。 | ✅ 可能 | ✅ 可能 | ✅ 可能 |
| `pinning` | デフォルト。`upgrade --all` から除外されるが、明示的 upgrade は可能。 | ❌ スキップ | ✅ 可能 | ✅ 可能 |
| `blocking` | `upgrade --all` から除外。明示的 upgrade も可能（v1.12.420 での動作）。 | ❌ スキップ | ✅ 可能※ | ✅ 可能※ |
| `gating` | 指定バージョン範囲内のみ upgrade 可能（例: `1.7.*`）。 | 範囲内のみ | 範囲内のみ | 範囲内のみ |

※ 公式ドキュメントでは blocking は明示的 upgrade もブロックとあるが、v1.12.420 では可能。

#### インストールオプション（winget パススルー）

| パラメータ | winget オプション | 説明 |
|-----------|------------------|------|
| `scope` | `--scope` | `user` または `machine` |
| `architecture` | `--architecture` | `x86`, `x64`, `arm`, `arm64` |
| `installerType` | `--installer-type` | インストーラータイプ |
| `interactive` | `--interactive` | 対話型インストール |
| `silent` | `--silent` | サイレントインストール |
| `locale` | `--locale` | ロケール（BCP47形式） |
| `location` | `--location` | インストール先パス |
| `log` | `--log` | ログファイルパス |
| `custom` | `--custom` | 追加のインストーラー引数 |
| `override` | `--override` | インストーラー引数の上書き |
| `force` | `--force` | 強制実行 |
| `acceptPackageAgreements` | `--accept-package-agreements` | パッケージ契約に同意 |
| `acceptSourceAgreements` | `--accept-source-agreements` | ソース契約に同意 |
| `allowHashMismatch` | `--ignore-security-hash` | ハッシュ不一致を無視 |
| `skipDependencies` | `--skip-dependencies` | 依存関係をスキップ |
| `header` | `--header` | カスタム HTTP ヘッダー |

### YAML 例

```yaml
# 最新版をインストール、アップグレード可能（ピン留めなし）
Microsoft.VisualStudioCode:
  scope: user
  silent: true
  override: /VERYSILENT /MERGETASKS=!runcode

# バージョン 23.01 に固定（upgrade --all から除外）
7zip.7zip:
  pin: "23.01"
  architecture: x64

# バージョン 1.7.x の範囲に制限（gating）
jqlang.jq:
  pin: "1.7.*"
  pinType: gating

# 完全固定（blocking）
CriticalApp.App:
  pin: "2.0.0"
  pinType: blocking

# アンインストール対象
DeepL.DeepL:
  uninstall: true
```

---

## コマンド仕様

### コマンド一覧

| コマンド | パススルー | Gist同期 | 概要 |
|---------|:----------:|:--------:|------|
| `auth login` | ❌ | ❌ | GitHub Device Flow でログイン。 |
| `auth logout` | ❌ | ❌ | GitHub からログアウト。 |
| `auth status` | ❌ | ❌ | 認証状態を表示。 |
| `sync` | ❌ | ✅ | Gist の `packages.yaml` とローカル状態を同期。差分を検出し、インストール/アンインストール/pin設定を実行。 |
| `install` | ❌ | ✅ | パッケージをインストールし、Gist に保存。`--id` 必須。インストール済みの場合はアップグレード。 |
| `uninstall` | ❌ | ✅ | パッケージをアンインストールし、Gist を更新（`uninstall: true` を設定）。`--id` 必須。 |
| `upgrade` | 条件付き | 条件付き | ID指定時: upgrade後にGist更新。ID未指定時: wingetにパススルー。 |
| `pin add` | ❌ | ✅ | パッケージをピン留めし、Gist に保存。`--version` 必須。 |
| `pin remove` | ❌ | ✅ | ピン留めを解除し、Gist を更新（`pin` を削除）。 |
| `pin list` | ✅ | ❌ | winget にパススルー。 |
| `pin reset` | ✅ | ❌ | winget にパススルー。 |
| `export` | ❌ | ❌ | ローカルのインストール済みパッケージを YAML 形式で出力。 |
| `import` | ❌ | ✅ | YAML ファイルを Gist にインポート。 |
| `list` | ✅ | ❌ | winget にパススルー。 |
| `search` | ✅ | ❌ | winget にパススルー。 |
| `show` | ✅ | ❌ | winget にパススルー。 |
| `source` | ✅ | ❌ | winget にパススルー。 |
| `settings` | ✅ | ❌ | winget にパススルー。 |
| `features` | ✅ | ❌ | winget にパススルー。 |
| `hash` | ✅ | ❌ | winget にパススルー。 |
| `validate` | ✅ | ❌ | winget にパススルー。 |
| `configure` | ✅ | ❌ | winget にパススルー。 |
| `download` | ✅ | ❌ | winget にパススルー。 |
| `repair` | ✅ | ❌ | winget にパススルー。 |

---

### コマンド詳細

#### sync

Gist の `packages.yaml` とローカルのパッケージ状態を同期します。

```
gistget sync [--url <gist-url>]
```

| オプション | 説明 |
|-----------|------|
| `--url` | 同期元の Gist URL。省略時は認証ユーザーの Gist を検索。指定時は読み取り専用モード。 |

**処理フロー:**

1. Gist から `packages.yaml` を取得
2. ローカルのインストール済みパッケージを取得（`winget list`）
3. 差分を計算:
   - Gist にあり、ローカルに未インストール → インストール対象
   - Gist で `uninstall: true`、ローカルにインストール済み → アンインストール対象
4. アンインストール実行（`winget uninstall`）
5. インストール実行（`winget install`）
6. pin の同期:
   - YAML に `pin` あり → `winget pin add --version <pin> [--blocking]`
   - YAML に `pin` なし → `winget pin remove`（存在すれば）
7. `--url` 省略時のみ、更新した `packages.yaml` を Gist に保存

**同期マトリクス:**

以下の表は、ローカル状態（縦軸）と Gist 状態（横軸）の組み合わせに対する sync の動作を定義します。

|  | Gist: エントリなし | Gist: `uninstall: true` | Gist: エントリあり（pin なし） | Gist: エントリあり（pin あり） |
|--|-------------------|------------------------|-------------------------------|-------------------------------|
| **ローカル: 未インストール** | 何もしない | 何もしない | インストール（最新版） | インストール（pin バージョン）+ pin追加 |
| **ローカル: インストール済み + pin なし** | 何もしない ※1 | アンインストール | 何もしない | pin追加 |
| **ローカル: インストール済み + pin あり（一致）** | 何もしない ※1 | アンインストール + pin削除 | pin削除 | 何もしない |
| **ローカル: インストール済み + pin あり（不一致）** | 何もしない ※1 | アンインストール + pin削除 | pin削除 | pin更新 |

※1: ローカルにのみ存在するパッケージは Gist に自動追加されません。明示的に `gistget install` または `gistget export` + `gistget import` を使用してください。

**pinType の同期:**

| Gist の pinType | 動作 |
|-----------------|------|
| `pinning`（または省略） | `winget pin add --version <pin>` |
| `blocking` | `winget pin add --version <pin> --blocking` |
| `gating` | `winget pin add --version <pin>`（ワイルドカード使用） |

**バージョン不一致時の動作:**

sync はバージョンの**アップグレード/ダウングレードを行いません**。pin の設定のみを同期します。

| 状況 | 動作 |
|------|------|
| Gist: `pin: "1.7"`, ローカル: v1.8 インストール済み | pin を `1.7` に設定（次回 upgrade --all から除外） |
| Gist: `pin: "2.0"`, ローカル: v1.5 インストール済み | pin を `2.0` に設定（アップグレードは行わない） |
| Gist: pin なし, ローカル: v1.7 + pin あり | pin を削除（upgrade --all の対象になる） |

バージョンを変更したい場合は、明示的に `gistget upgrade --id <id> --pin <version>` を実行してください。

---

#### install

パッケージをインストールし、`packages.yaml` を更新して Gist に保存します。

```
gistget install --id <package-id> [--version <version>] [options]
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `--id` | ✅ | パッケージ ID |
| `--version` | ❌ | インストールするバージョン。省略時は最新版。 |
| その他 | ❌ | winget install オプション（`--scope`, `--silent` 等） |

**winget install の重要な動作:**

- デフォルトでは、インストール済みパッケージに対して install を実行すると**アップグレード**が行われる
- `--no-upgrade` オプションでアップグレードをスキップ可能
- `--version` 指定時、インストール済みバージョンと異なる場合はそのバージョンに変更される

**同期マトリクス:**

|  | Gist: エントリなし | Gist: エントリあり |
|--|-------------------|-------------------|
| **ローカル: 未インストール** | インストール → Gist に追加 | インストール → Gist 更新 |
| **ローカル: インストール済み（同バージョン）** | Gist に追加 ※1 | 何もしない（`--no-upgrade` 使用） |
| **ローカル: インストール済み（旧バージョン）** | アップグレード → Gist に追加 | アップグレード → Gist 更新 |

※1: `--version` 指定なしの場合。`--version` 指定時はそのバージョンに変更。

**処理フロー:**

1. Gist から `packages.yaml` を取得
2. `winget install --id <id> [--version <version>] [options]` を実行
3. 失敗時はエラー終了
4. 成功時:
   - Gist に既存の `pin` がある場合は `winget pin add --id <id> --version <pin> [--blocking]` でローカルに同期
   - `packages.yaml` にエントリを追加/更新（インストールオプションを保存、`pin` が存在する場合のみ `version` も保存）
5. Gist に `packages.yaml` を保存

**注意:**
- `--id` は必須。`--query` や `--name` による曖昧な指定はエラー。
- Gist に pin が存在する場合は install 時にローカルへ pin を同期する。pin が不要な場合は `gistget pin remove` を使用する。
- バージョンを指定しても pin を指定しない限り YAML には `version` を保存しない。

---

#### uninstall

パッケージをアンインストールし、`packages.yaml` を更新して Gist に保存します。

```
gistget uninstall --id <package-id>
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `--id` | ✅ | パッケージ ID |

**処理フロー:**

1. Gist から `packages.yaml` を取得
2. `winget uninstall --id <id>` を実行
3. 失敗時はエラー終了
4. 成功時:
   - `packages.yaml` の該当エントリに `uninstall: true` を設定
   - `winget pin remove <id>` を実行（pin が存在すれば）
5. Gist に `packages.yaml` を保存

**注意:**
- アンインストールしてもエントリは削除されず、`uninstall: true` が設定される。
- これにより、他のデバイスで `sync` 実行時に同じパッケージがアンインストールされる。
- エントリを完全に削除するには、Gist を直接編集する。
- **重要**: `winget uninstall` は pin を自動削除しないため、明示的に `winget pin remove` を実行する。

---

#### upgrade

パッケージをアップグレードし、`packages.yaml` を更新して Gist に保存します。

```
gistget upgrade [<package-id>] [--id <package-id>] [options]
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `<package-id>` | ❌ | パッケージ ID（位置引数） |
| `--id` | ❌ | パッケージ ID（オプション形式） |
| `--version` | ❌ | アップグレード先バージョン（winget に渡す） |

**動作分岐:**

| ID指定 | 動作 | Gist同期 |
|:------:|------|:--------:|
| あり | パッケージを upgrade し、Gist を更新 | ✅ |
| なし | winget にパススルー（`upgrade --all` 等） | ❌ |

**同期マトリクス（ID 指定時）:**

以下の表は、ローカル状態（縦軸）と Gist 状態（横軸）の組み合わせに対する upgrade の動作を定義します。

|  | Gist: エントリなし | Gist: エントリあり（pin なし） | Gist: エントリあり（pin あり） |
|--|-------------------|-------------------------------|-------------------------------|
| **ローカル: 未インストール** | エラー ※1 | エラー ※1 | エラー ※1 |
| **ローカル: インストール済み + pin なし** | upgrade → Gist に追加 | upgrade のみ ※2 | upgrade → Gist の pin を新バージョンに更新 + ローカル pin 更新 |
| **ローカル: インストール済み + pin あり** | upgrade → Gist に追加（pin 含む）→ ローカル pin 更新 | upgrade → Gist に pin 追加 → ローカル pin 更新 | upgrade → Gist の pin を新バージョンに更新 → ローカル pin 更新 |

※1: winget upgrade は未インストールパッケージに対してエラーを返す。  
※2: Gist に既にエントリがあり、pin 指定もない場合は YAML 変更なし（最新追従の状態を維持）。

**Gist を更新する意図:**

1. **pin の同期維持**: ローカルで pin されたバージョンを upgrade した場合、pin を新バージョンに更新する必要がある
2. **新規エントリの登録**: Gist に未登録のパッケージを upgrade した場合、エントリを追加して管理対象にする
3. **複数デバイス間の一貫性**: upgrade 結果を他のデバイスに反映可能にする

**処理フロー:**

1. `winget upgrade --id <id> [options]` を実行
2. 失敗時はエラー終了
3. 成功時:
   - Gist から `packages.yaml` を取得
   - エントリがなければ追加
   - ローカルに pin がある場合:
     - `winget pin add --id <id> --version <新バージョン> --force` で pin を更新
     - Gist の `pin` を新バージョンに更新
4. Gist に `packages.yaml` を保存

**注意:**
- pin がある状態で明示的に upgrade すると、pin のバージョンも新しいバージョンに自動更新される（winget の動作）。
- ID 未指定時は `winget upgrade` にそのままパススルーされ、Gist 同期は行われない。
- pin の新規設定・変更は `gistget pin add` コマンドで行う。upgrade は既存 pin の追従のみ。

---

#### pin add

パッケージをピン留めし、`packages.yaml` を更新して Gist に保存します。

```
gistget pin add <package-id> --version <version>
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `<package-id>` | ✅ | パッケージ ID |
| `--version` | ✅ | ピン留めするバージョン（ワイルドカード `*` 使用可） |

**備考:**
- 現状の CLI では `pinType`（blocking / gating）の指定オプションは提供しない。
- 既に `packages.yaml` に `pinType` がある場合はその値を維持し、winget 実行時も該当フラグ（`--blocking` / `--gating`）を付与する。
- `pinType` 未指定の場合は省略（= pinning）として扱う。

**処理フロー:**

1. Gist から `packages.yaml` を取得
2. `winget pin add --id <id> --version <version> --force` を実行（`pinType` に応じて `--blocking` / `--gating` を付与）
3. 失敗時はエラー終了
4. 成功時:
   - `packages.yaml` の `pin`（および `version`）を更新し、`pinType` は既存値を維持
5. Gist に `packages.yaml` を保存

---

#### pin remove

ピン留めを解除し、`packages.yaml` を更新して Gist に保存します。

```
gistget pin remove <package-id>
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `<package-id>` | ✅ | パッケージ ID |

**処理フロー:**

1. Gist から `packages.yaml` を取得
2. `winget pin remove --id <id>` を実行
3. 失敗時はエラー終了
4. 成功時:
   - `packages.yaml` から `pin`、`pinType`、`version` を削除
5. Gist に `packages.yaml` を保存

**注意:**
- エントリ自体は削除されず、pin 関連フィールドのみが削除される。
- `uninstall` 等の他のフィールドは保持される。

---

## 重要な動作仕様

### pin に関する注意事項

winget v1.12.420 での検証結果に基づく動作仕様:

1. **バージョン指定インストールは pin を追加しない**
   - `winget install --version` だけでは pin されない
   - pin するには明示的に `winget pin add` が必要
   - バージョン指定でインストールしても、以降の `winget upgrade` や `winget upgrade --all` でアップグレード対象になる

2. **アンインストールしても pin は残る**
   - `winget uninstall` は pin を自動削除しない
   - GistGet では `uninstall` 時に明示的に `winget pin remove` を実行する

3. **明示的 upgrade 時に pin は維持される**
   - pin がある状態で `winget upgrade <id>` すると、upgrade は成功する
   - pin のバージョンは新しいバージョンに自動更新される

4. **Pinning vs Blocking の実際の動作**
   - 公式ドキュメントでは blocking は `winget upgrade <id>` もブロックするとあるが、v1.12.420 では明示的 upgrade が可能
   - 実質的に pinning と blocking の違いは `upgrade --all --include-pinned` の動作のみ

### バージョン固定の推奨手順

特定バージョンに固定したい場合:

```powershell
# Step 1: バージョン指定でインストール
winget install jqlang.jq --version 1.7

# Step 2: インストール済みバージョンをpin
winget pin add jqlang.jq --installed --blocking
```

### Pin後のバージョン変更

pinを削除せずに別バージョンへアップグレードした場合:

```powershell
# Blocking pinがある状態で
winget upgrade jqlang.jq --version 1.8.0
```

- アップグレードが**成功する**（v1.12.420での検証結果）
- pinのバージョンも**新しいバージョンに自動更新**される

正式な手順:

```powershell
# 1. pinを削除
winget pin remove jqlang.jq

# 2. アップグレード
winget upgrade jqlang.jq --version 1.8.0

# 3. 新しいバージョンで再度pin
winget pin add jqlang.jq --installed --blocking
```

### Pinの確認・削除コマンド

```powershell
# 全てのpinを一覧表示
winget pin list

# 特定パッケージのpin確認
winget pin list --id jqlang.jq

# 特定パッケージのpin削除
winget pin remove jqlang.jq

# 全てのpinをリセット（確認のみ）
winget pin reset

# 全てのpinを強制リセット
winget pin reset --force
```

### sync の冪等性

`sync` コマンドは何度実行しても同じ結果になることを保証します:

- Gist と ローカルが一致していれば何も実行しない
- 差分がある場合のみ必要な操作を実行
- エラーが発生しても部分的に適用された変更は記録される

### エラーハンドリング

- winget コマンドが失敗した場合、Gist への保存は行わない
- 複数パッケージの sync 中にエラーが発生しても、他のパッケージの処理は継続する
- エラーは最後にまとめて報告する

---

## 参考リンク

- [winget pin コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/pinning)
- [winget upgrade コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/upgrade)
- [winget install コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/install)
- [GistGet PIN 挙動検証](./PIN.ja.md)
