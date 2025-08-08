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

#### B. 同期ドメイン

* Gist に保存されたファイルをもとに設定を同期する
* [PowerShell版](./powershell/)の実装に準拠する

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


[1]: https://github.com/microsoft/winget-cli/issues/4320?utm_source=chatgpt.com "Issues with COM API and retrieving installed packages"
[2]: https://www.nuget.org/packages/Microsoft.WindowsPackageManager.ComInterop?utm_source=chatgpt.com "Microsoft.WindowsPackageManager.ComInterop 1.11.430"
[3]: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps?utm_source=chatgpt.com "Authorizing OAuth apps - GitHub Docs"
[4]: https://docs.github.com/enterprise-cloud%40latest/apps/creating-github-apps/writing-code-for-a-github-app/building-a-cli-with-a-github-app?utm_source=chatgpt.com "Building a CLI with a GitHub App"
[5]: https://learn.microsoft.com/en-us/windows/package-manager/winget/?utm_source=chatgpt.com "Use WinGet to install and manage applications"
[6]: https://www.reddit.com/r/golang/comments/17m22mq/github_oauth2_device_flow_does_anyone_have_an/?utm_source=chatgpt.com "github oauth2 device flow. does anyone have an example?"
[7]: https://github.com/microsoft/winget-cli/issues/4377?utm_source=chatgpt.com "WinRTAct.dll from Microsoft.WindowsPackageManager. ..."
