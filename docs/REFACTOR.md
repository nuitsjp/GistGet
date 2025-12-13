# refactor ブランチ TODO（main との差分整理）

main → refactor で入った変更を棚卸し。チェック済みは反映済み、未チェックはフォローアップ。

## 実施済み
- [x] ビルド/パッケージ管理: `Directory.Build.props` を追加して Nullable/ImplicitUsings/LangVersion=preview/Platforms を共通化し、ターゲットを net10.0-windows10.0.26100.0 に更新。`Directory.Packages.props` で中央パッケージ管理を導入し、各 csproj から個別バージョン指定を削減（テスト csproj から CsWinRT/ComInterop 参照も削除）。
- [x] ソリューション/CI: ルートの `GistGet.slnx`/`GistGet_old.slnx` と `.DotSettings` を削除して `src/GistGet.slnx` に集約。CI は main ブランチのみをトリガーし、SOLUTION_FILE を `src/GistGet.slnx` に変更。
- [x] CLI upgrade: `UpgradeOptions` と `winget upgrade` 引数生成で `--header` をサポートし、CLI にも同オプションを追加。
- [x] Gist/GitHub: 既定 Gist ファイルを `GistGet.yaml` に変更し、GitHubService はファイル名一致でページング検索し、未存在ならプライベート Gist を新規作成して空 YAML を置く挙動に変更（description マッチや複数検出エラーを廃止）。
- [x] WinGet 引数: `BuildUpgradeArgs` でも `Header` を共通オプションとして渡すよう統一。
- [x] CLI sync: `SyncAsync` が `--file` でローカル YAML を優先読込（存在チェック付き）し、同期処理にインストール/アンインストール/pin の進捗ログを追加。
- [x] テスト: Sync の進捗ログ（インストール/アンインストール/pin）検証を追加し、CredentialService 既定ターゲットの保存/取得テストを追加。WinGetServiceTests に Integration trait を付与。テスト csproj も net10/中央パッケージ管理に追従。
- [x] ドキュメント: README/SPEC/DEVELOPER/DESIGN を `GistGet.yaml` 前提、net10 依存、`sync --file` 例などへ更新（文章・構成を簡潔化）。

## フォローアップ
- [ ] CI の DOTNET_VERSION (8.0.x のまま) を net10/C# preview に合わせる。必要なら vs-buildtools/SDK のセットアップも見直す。
- [x] ルートソリューション削除に伴い、`AGENTS.md` のビルド手順や `scripts/Run-Tests.ps1` 等が参照する `GistGet.sln`/`GistGet.slnx` パスを `src/GistGet.slnx` に更新。
- [x] `packages.yaml` 参照が残るコメント/ドキュメント（例: IGistGetService, IGitHubService, GistGetPackageSerializer など）を `GistGet.yaml` に統一。
- [x] SPEC/テストに `sync --file` と `upgrade --header` の挙動を追加で明記/カバー（WinGetArgumentBuilderTests では upgrade 時の Header を検証済み）。
- [ ] GitHubService の新しい Gist 自動作成/ファイル名優先検索の仕様を設計ドキュメントに反映し、複数 Gist 併存時の扱いをレビュー。

