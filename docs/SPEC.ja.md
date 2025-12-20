# GistGet 仕様書

GistGetは、Windows Package Manager(winget)のパッケージ管理状態をGitHub Gistを介してクラウド同期するツールです。

> **対応 winget バージョン**: v1.12.420 以降  
> **最終更新**: 2025年12月

## 設計原則

1. **wingetの透過的なラッパー**: wingetのオプションを極力そのままパススルーする
2. **Gist を単一情報源 (Single Source of Truth)**: `GistGet.yaml`（説明: "GistGet Packages"）がすべてのデバイスで共有される正規の状態
3. **明示的な ID 指定**: 曖昧な `--query` や `--name` は禁止し、`--id` による一意な指定を必須とする
4. **シンプルなバージョン管理**: pinする場合のみバージョンを管理し、それ以外は常に最新版を使用

---

## YAML スキーマ仕様

### 概要

`GistGet.yaml`は、パッケージIDをキーとし、インストールオプションと同期フラグを値とするマップです。

```yaml
<PackageId>:
  name: <string>                   # winget の表示名（自動設定）
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

### パラメーター詳細

#### コアパラメーター

| パラメーター | 型 | 説明 |
|-----------|-----|------|
| `name` | string | winget が表示するパッケージ名。`install` / `upgrade` / `uninstall` / `pin add` / `init` で自動設定される。手動編集時も必須。 |
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

※公式ドキュメントではblockingは明示的upgradeもブロックとあるが、v1.12.420では可能。

#### インストールオプション(wingetパススルー)

| パラメーター | wingetオプション | 説明 |
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
  name: Visual Studio Code
  scope: user
  silent: true
  override: /VERYSILENT /MERGETASKS=!runcode

# バージョン 23.01 に固定（upgrade --all から除外）
7zip.7zip:
  name: 7-Zip
  pin: "23.01"
  architecture: x64

# バージョン 1.7.x の範囲に制限（gating）
jqlang.jq:
  name: jq
  pin: "1.7.*"
  pinType: gating

# 完全固定（blocking）
CriticalApp.App:
  name: CriticalApp
  pin: "2.0.0"
  pinType: blocking

# アンインストール対象
DeepL.DeepL:
  name: DeepL
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
| `sync` | ❌ | ✅ | Gistの`GistGet.yaml`とローカル状態を同期。差分を検出し、インストール/アンインストール/pin設定を実行。 |
| `init` | ❌ | ✅ | ローカルのインストール済みパッケージを対話的に選択し、Gistを初期化。 |
| `install` | ❌ | ✅ | パッケージをインストールし、Gistに保存。`--id`必須。インストール済みの場合はアップグレード。 |
| `uninstall` | ❌ | ✅ | パッケージをアンインストールし、Gistを更新(`uninstall: true`を設定)。`--id`必須。 |
| `upgrade` | 条件付き | 条件付き | ID指定時: upgrade後にGist更新。ID未指定時: wingetにパススルー。 |
| `pin add` | ❌ | ✅ | パッケージをピン留めし、Gistに保存。`--version`必須。`--blocking` / `--gating` / `--force`を指定可能。 |
| `pin remove` | ❌ | ✅ | ピン留めを解除し、Gistを更新(`pin`を削除)。 |
| `pin list` | ✅ | ❌ | wingetにパススルー。 |
| `pin reset` | ✅ | ❌ | wingetにパススルー。 |
| `list` | ✅ | ❌ | wingetにパススルー。 |
| `search` | ✅ | ❌ | wingetにパススルー。 |
| `show` | ✅ | ❌ | wingetにパススルー。 |
| `source` | ✅ | ❌ | wingetにパススルー。 |
| `settings` | ✅ | ❌ | wingetにパススルー。 |
| `features` | ✅ | ❌ | wingetにパススルー。 |
| `hash` | ✅ | ❌ | wingetにパススルー。 |
| `validate` | ✅ | ❌ | wingetにパススルー。 |
| `configure` | ✅ | ❌ | wingetにパススルー。 |
| `download` | ✅ | ❌ | wingetにパススルー。 |
| `repair` | ✅ | ❌ | wingetにパススルー。 |
| `dscv3` | ✅ | ❌ | wingetにパススルー。 |
| `mcp` | ✅ | ❌ | wingetにパススルー。 |

---

### コマンド詳細

#### sync

Gistの`GistGet.yaml`とローカルのパッケージ状態を同期します。

```
gistget sync [--url <yaml-url>]
```

| オプション | 説明 |
|-----------|------|
| `--url` | 同期元のYAML URL。GistのRaw URLやその他のHTTP/HTTPS URLを指定可能。省略時は認証ユーザーのGistを使用。 |

**処理フロー:**

1. Gistから`GistGet.yaml`を取得
2. ローカルのインストール済みパッケージを取得（`winget list`）
3. 差分を計算:
   - Gistにあり、ローカルに未インストール → インストール対象
   - Gistで`uninstall: true`、ローカルにインストール済み → アンインストール対象
4. アンインストール実行（`winget uninstall`）
5. インストール実行（`winget install`）
6. pinの同期:
   - YAMLに`pin`あり → `winget pin add --version <pin> [--blocking | --gating]`
   - YAMLに`pin`なし → `winget pin remove`（存在すれば）
7. `--url`省略時のみ、更新した`GistGet.yaml`をGistに保存

> [!NOTE]
> syncは**Gist → ローカルの片方向同期**です。ローカルにのみ存在するパッケージはGistに書き戻されません。

**同期マトリクス:**

以下の表は、ローカル状態（縦軸）とGist状態（横軸）の組み合わせに対するsyncの動作を定義します。

|  | Gist: エントリなし | Gist: `uninstall: true` | Gist: エントリあり（pin なし） | Gist: エントリあり（pin あり） |
|--|-------------------|------------------------|-------------------------------|-------------------------------|
| **ローカル: 未インストール** | 何もしない | 何もしない | インストール（最新版） | インストール（pin バージョン）+ pin追加 |
| **ローカル: インストール済み + pin なし** | 何もしない ※1 | アンインストール | 何もしない | pin追加 |
| **ローカル: インストール済み + pin あり（一致）** | 何もしない ※1 | アンインストール + pin削除 | pin削除 | 何もしない |
| **ローカル: インストール済み + pin あり（不一致）** | 何もしない ※1 | アンインストール + pin削除 | pin削除 | pin更新 |

※1: ローカルにのみ存在するパッケージはGistに自動追加されません。明示的に`gistget install`を実行するか、Gist上の`GistGet.yaml`を直接編集してください。

**pinTypeの同期:**

| GistのpinType | 動作 |
|-----------------|------|
| `pinning`(または省略) | `winget pin add --version <pin>` |
| `blocking` | `winget pin add --version <pin> --blocking` |
| `gating` | `winget pin add --version <pin>`(ワイルドカード使用) |

**バージョン不一致時の動作:**

syncはバージョンの**アップグレード/ダウングレードを行いません**。pinの設定のみを同期します。

| 状況 | 動作 |
|------|------|
| Gist: `pin: "1.7"`, ローカル: v1.8インストール済み | pinを`1.7`に設定(次回upgrade --allから除外) |
| Gist: `pin: "2.0"`, ローカル: v1.5インストール済み | pinを`2.0`に設定(アップグレードは行わない) |
| Gist: pinなし, ローカル: v1.7 + pinあり | pinを削除(upgrade --allの対象になる) |

バージョンを変更したい場合は、明示的に`gistget upgrade --id <id> --version <version>`を実行してください。

---

#### init

ローカルにインストールされているパッケージを対話的に選択し、Gistの`GistGet.yaml`を初期化します。

```
gistget init
```

**処理フロー:**

1. ローカルにインストールされているパッケージをすべて取得
2. 各パッケージについて、同期対象に含めるか y/N で確認
3. すべての確認が終了後、最終的な上書き確認を表示
4. 選択されたパッケージで Gist の `GistGet.yaml` を**完全に上書き**

**注意:**
- **既存の Gist 内容はマージされず、完全に上書きされる。**
- 認証が必要。未認証の場合はログインを促す。

---

#### install

パッケージをインストールし、`GistGet.yaml`を更新してGistに保存します。

```
gistget install --id <package-id> [--version <version>] [options]
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `--id` | ✅ | パッケージID |
| `--version` | ❌ | インストールするバージョン。省略時は最新版。 |
| その他 | ❌ | winget installオプション(`--scope`, `--silent`等) |

**winget installの重要な動作:**

- デフォルトでは、インストール済みパッケージに対してinstallを実行すると**アップグレード**が行われる
- `--no-upgrade`オプションでアップグレードをスキップ可能
- `--version`指定時、インストール済みバージョンと異なる場合はそのバージョンに変更される

**同期マトリクス:**

|  | Gist: エントリなし | Gist: エントリあり |
|--|-------------------|-------------------|
| **ローカル: 未インストール** | インストール → Gistに追加 | インストール → Gist更新 |
| **ローカル: インストール済み(同バージョン)** | Gistに追加 ※1 | 何もしない(`--no-upgrade`使用) |
| **ローカル: インストール済み(旧バージョン)** | アップグレード → Gistに追加 | アップグレード → Gist更新 |

※1: `--version`指定なしの場合。`--version`指定時はそのバージョンに変更。

**処理フロー:**

1. Gistから`GistGet.yaml`を取得
2. `winget install --id <id> [--version <version>] [options]`を実行
3. 失敗時はエラー終了
4. 成功時:
   - Gistに既存の`pin`がある場合は`winget pin add --id <id> --version <pin> [--blocking | --gating]`でローカルに同期
   - `winget show`/`list` の結果から表示名を取得し、`name` を保存
   - `GistGet.yaml`にエントリを追加/更新（インストールオプションを保存、`pin`が存在する場合のみ`version`も保存）
5. Gistに`GistGet.yaml`を保存

**注意:**
- `--id`は必須。`--query`や`--name`による曖昧な指定はエラー。
- Gistにpinが存在する場合はinstall時にローカルへpinを同期する。pinが不要な場合は`gistget pin remove`を使用する。
- バージョンを指定してもpinを指定しない限りYAMLには`version`を保存しない。

---

#### uninstall

パッケージをアンインストールし、`GistGet.yaml`を更新してGistに保存します。

```
gistget uninstall --id <package-id>
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `--id` | ✅ | パッケージID |

**処理フロー:**

1. Gistから`GistGet.yaml`を取得
2. `winget uninstall --id <id>`を実行
3. 失敗時はエラー終了
4. 成功時:
   - `GistGet.yaml`の該当エントリに`uninstall: true`を設定
   - `winget show`/`list` の結果から表示名を取得し、`name` を保存（既存エントリに名前がある場合は維持）
   - `winget pin remove <id>`を実行（pinが存在すれば）
5. Gistに`GistGet.yaml`を保存

**注意:**
- アンインストールしてもエントリは削除されず、`uninstall: true`が設定される。
- これにより、他のデバイスで`sync`実行時に同じパッケージがアンインストールされる。
- エントリを完全に削除するには、Gistを直接編集する。
- **重要**: `winget uninstall`はpinを自動削除しないため、明示的に`winget pin remove`を実行する。

---

#### upgrade

パッケージをアップグレードし、`GistGet.yaml`を更新してGistに保存します。

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

以下の表は、ローカル状態（縦軸）とGist状態（横軸）の組み合わせに対するupgradeの動作を定義します。

|  | Gist: エントリなし | Gist: エントリあり（pin なし） | Gist: エントリあり（pin あり） |
|--|-------------------|-------------------------------|-------------------------------|
| **ローカル: 未インストール** | エラー ※1 | エラー ※1 | エラー ※1 |
| **ローカル: インストール済み + pin なし** | upgrade → Gist に追加 | upgrade のみ ※2 | upgrade → Gist の pin を新バージョンに更新 + ローカル pin 更新 |
| **ローカル: インストール済み + pin あり** | upgrade → Gist に追加（pin 含む）→ ローカル pin 更新 | upgrade → Gist に pin 追加 → ローカル pin 更新 | upgrade → Gist の pin を新バージョンに更新 → ローカル pin 更新 |

※1: winget upgradeは未インストールパッケージに対してエラーを返す。  
※2: Gistにすでにエントリがあり、pin指定もない場合はYAML変更なし（最新追従の状態を維持）。

**Gist を更新する意図:**

1. **pinの同期維持**: ローカルでpinされたバージョンをupgradeした場合、pinを新バージョンに更新する必要がある
2. **新規エントリの登録**: Gistに未登録のパッケージをupgradeした場合、エントリを追加して管理対象にする
3. **複数デバイス間の一貫性**: upgrade結果を他のデバイスに反映可能にする

**処理フロー:**

1. `winget upgrade --id <id> [options]`を実行
2. 失敗時はエラー終了
3. 成功時:
    - Gistから`GistGet.yaml`を取得
    - エントリがなければ追加
    - ローカルにpinがある場合:
       - `winget pin add --id <id> --version <新バージョン> --force`でpinを更新
       - Gistの`pin`を新バージョンに更新
4. Gistに`GistGet.yaml`を保存

**注意:**
- pinがある状態で明示的にupgradeすると、pinのバージョンも新しいバージョンに自動更新される（wingetの動作）。
- ID未指定時は`winget upgrade`にそのままパススルーされ、Gist同期は行われない。
- pinの新規設定・変更は`gistget pin add`コマンドで行う。upgradeは既存pinの追従のみ。

---

#### pin add

パッケージをピン留めし、`GistGet.yaml`を更新してGistに保存します。

```
gistget pin add <package-id> --version <version>
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `<package-id>` | ✅ | パッケージID |
| `--version` | ✅ | ピン留めするバージョン（ワイルドカード`*`使用可） |
| `--blocking` | ❌ | pinTypeをblockingとして追加 |
| `--gating` | ❌ | pinTypeをgatingとして追加 |
| `--force` | ❌ | すでにpinが存在する場合に強制上書き |

**備考:**
- CLIでblocking/gatingを明示指定可能。両方指定した場合は後勝ちではなく、排他で運用すること。
- すでに`GistGet.yaml`に`pinType`がある場合は指定がなければその値を維持し、winget実行時も該当フラグ（`--blocking` / `--gating`）を付与する。
- `pinType`未指定の場合は省略（= pinning）として扱う。

**処理フロー:**

1. Gistから`GistGet.yaml`を取得
2. `winget pin add --id <id> --version <version> --force`を実行（`pinType`に応じて`--blocking` / `--gating`を付与、CLI指定があればそれを優先）
3. 失敗時はエラー終了
4. 成功時:
   - `GistGet.yaml`の`pin`（および`version`）を更新し、`pinType`は既存値を維持（CLI指定があれば上書き）
5. Gistに`GistGet.yaml`を保存

---

#### pin remove

ピン留めを解除し、`GistGet.yaml`を更新してGistに保存します。

```
gistget pin remove <package-id>
```

| オプション | 必須 | 説明 |
|-----------|:----:|------|
| `<package-id>` | ✅ | パッケージ ID |

**処理フロー:**

1. Gistから`GistGet.yaml`を取得
2. `winget pin remove --id <id>`を実行
3. 失敗時はエラー終了
4. 成功時:
   - `GistGet.yaml`から`pin`、`pinType`、`version`を削除
5. Gistに`GistGet.yaml`を保存

**注意:**
- エントリ自体は削除されず、pin関連フィールドのみが削除される。
- `uninstall` 等の他のフィールドは保持される。

---

## 重要な動作仕様

### pin に関する注意事項

winget v1.12.420での検証結果に基づく動作仕様:

1. **バージョン指定インストールはpinを追加しない**
   - `winget install --version`だけではpinされない
   - pinするには明示的に`winget pin add`が必要
   - バージョン指定でインストールしても、以降の`winget upgrade`や`winget upgrade --all`でアップグレード対象になる

2. **アンインストールしてもpinは残る**
   - `winget uninstall`はpinを自動削除しない
   - GistGetでは`uninstall`時に明示的に`winget pin remove`を実行する

3. **明示的upgrade時にpinは維持される**
   - pinがある状態で`winget upgrade <id>`すると、upgradeは成功する
   - pinのバージョンは新しいバージョンに自動更新される

4. **Pinning vs Blockingの実際の動作**
   - 公式ドキュメントではblockingは`winget upgrade <id>`もブロックするとあるが、v1.12.420では明示的upgradeが可能
   - 実質的にpinningとblockingの違いは`upgrade --all --include-pinned`の動作のみ

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

- Gistとローカルが一致していれば何も実行しない
- 差分がある場合のみ必要な操作を実行
- エラーが発生しても部分的に適用された変更は記録される

### エラーハンドリング

- wingetコマンドが失敗した場合、Gistへの保存は行わない
- 複数パッケージのsync中にエラーが発生しても、他のパッケージの処理は継続する
- エラーは最後にまとめて報告する

---

## 参考リンク

- [winget pin コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/pinning)
- [winget upgrade コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/upgrade)
- [winget install コマンド (Microsoft Learn)](https://learn.microsoft.com/ja-jp/windows/package-manager/winget/install)
- [GistGet PIN 挙動検証](./PIN.ja.md)
