## 【.NET 8 + WinGet COM ラッパー & Gist 認証付き同期モジュール】設計

### 1. 背景と目的

* **課題**: WinGet を利用し、異なる環境間でパッケージと設定（ソース、構成含む）を同期したいが、WinGet は標準でサポートしていない。
* **目的**:

  * .NET 8（自己完結型）で WinGet の COM API をラップし、信頼性の高い同期ツールを構築。
  * 同期設定（パッケージ一覧など）を GitHub Gist に保存し、OAuth Device Flow による認証で安全に自動読み書き。

---

### 2. アーキテクチャ概要

#### A. WinGet COM ラッパー層（.NET 8 前提）

* **NuGet パッケージ**: `Microsoft.WindowsPackageManager.ComInterop` バージョン 1.11.430 を使用（.NET 8 互換）([GitHub][1], [nuget.org][2])。
* **API設計**:

  * `IWinGetClient` インターフェイスによる操作定義：検索、インストール、アップグレード、アンインストール、ソース操作、設定管理など。
  * COM 呼び出しはこのラッパー内で完結。
  * 必要に応じて CLI フォールバックレイヤーも設置。

#### B. 同期ドメイン（SyncEngine）

* Gist に保存された JSON/YAML → 差分計算 → COM API を用いて WinGet 操作を実行。
* 同期対象:

  * `packages.json`（export 互換）
  * `sources.json`（source export）
  * オプションで WinGet Configuration（YAML）

#### C. 認証（GitHub Gist への安全なアクセス）

* **認証方式**: OAuth App + Device Flow によるトークン取得（PAT 自動作成は不可）([GitHub Docs][3])。
* フロー:

  1. `device_code` と `verification_uri` を取得し、ブラウザを起動。
  2. ユーザーが認証後、アプリは `access_token` をポーリングで取得。
  3. 以降は `Authorization: Bearer <token>` で Gist API 呼び出し。
* スコープ: `gist`（公開・Secret Gist 読み書き可能）([GitHub Docs][3])。
* 実装参考: GitHub の Device Flow 手順および GitHub App 向けサンプル CLI ([GitHub Docs][4])。

#### D. トークン保存と管理

* アクセストークンは **Windows DPAPI** などでユーザー領域に暗号化保存。
* トークンが未設定／期限切れの場合、自動トークン再取得（Device Flow 再実行）。

---

### 3. 実装ステップ（ローディング順）

| ステップ | 内容                                                                                                                             |
| ---- | ------------------------------------------------------------------------------------------------------------------------------ |
| 1⃣   | .NET 8 プロジェクト設定 <br>– `net8.0-windows10.0.26100` ターゲット。                                                                        |
| 2⃣   | WinGet COM ラッパー実装 <br>– NuGet 1.11.430 導入、`IWinGetClient` 層実装。                                                                 |
| 3⃣   | OAuth Device Flow 機構追加 <br>– トークン取得（Device Flow）、トークン保存。                                                                       |
| 4⃣   | Gist 操作モジュール <br>– 読み書き、JSON/YAML 操作。                                                                                          |
| 5⃣   | SyncEngine 構築 <br>– 差分抽出と Plan → 実行。                                                                                           |
| 6⃣   | E2E テスト整備 <br>– Windows Sandbox などで export/import/Azure 上でテスト。                                                                 |
| 7⃣   | パッケージ化 <br>– self-contained 配布形式にまとめ、winget‑pkgs に公開マニフェスト PR。([Microsoft Learn][5], [Reddit][6], [GitHub][7], [nuget.org][2]) |

---

### 4. 注意点と技術的検討

* **App Installer（WinGet クライアント）依存**は継続（特に Server 環境）([Microsoft Learn][5])。
* **ユーザーコンテキスト依存**: SYSTEM やサービスとしての利用には別対応が必要（後回し推奨）。
* **API の互換性**: winget の更新により挙動が変わる可能性あり → **バージョン固定＋自動回帰テスト**が必要。
* **Device Flow 固有エラー対策**: `interval` 調整、`slow_down` などのエラー処理必須([GitHub Docs][3])。

---

### 5. 技術選定図案（簡易フロー図・モジュール構成）

```
[Entry Point / CLI]
       │
       ▼
[AuthManager] ←─(DPAPI 保存)─←──[OAuth Device Flow]
       │
       ▼
[GistStore] ←─(GitHub Gist API + Bearer Token)
       │
       ▼
[SyncEngine] ── extract plan → calls → [WinGetClient (COM)]
       │
       ▼
[WinGet COM API] (package install/export/configure)
```

---

ご希望があれば、この Canvas をもとに **README ドキュメントの Markdown 化**や **コード雛形生成**も可能です。どちらから進めましょうか？

[1]: https://github.com/microsoft/winget-cli/issues/4320?utm_source=chatgpt.com "Issues with COM API and retrieving installed packages"
[2]: https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop?utm_source=chatgpt.com "Microsoft.WindowsPackageManager.ComInterop 1.11.430"
[3]: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps?utm_source=chatgpt.com "Authorizing OAuth apps - GitHub Docs"
[4]: https://docs.github.com/enterprise-cloud%40latest/apps/creating-github-apps/writing-code-for-a-github-app/building-a-cli-with-a-github-app?utm_source=chatgpt.com "Building a CLI with a GitHub App"
[5]: https://learn.microsoft.com/en-us/windows/package-manager/winget/?utm_source=chatgpt.com "Use WinGet to install and manage applications"
[6]: https://www.reddit.com/r/golang/comments/17m22mq/github_oauth2_device_flow_does_anyone_have_an/?utm_source=chatgpt.com "github oauth2 device flow. does anyone have an example?"
[7]: https://github.com/microsoft/winget-cli/issues/4377?utm_source=chatgpt.com "WinRTAct.dll from Microsoft.WindowsPackageManager. ..."
