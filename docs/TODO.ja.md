# TODO（仕様準拠・整合性）

このファイルは、`docs/SPEC.ja.md` と現状実装の差分（仕様不一致/実装漏れ/設計ガイドライン逸脱）を一旦すべて列挙するためのバックログです。

## 最優先（ユーザー影響が大きい）

- [ ] `sync` が未実装（コマンドはあるがハンドラ未設定）: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`
- [ ] `export` / `import` が未実装（コマンドはあるがハンドラ未設定）: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`
- [ ] `IGistGetService` に `sync/export/import` が存在しない: `src/GistGet/GistGet/IGistGetService.cs`
- [ ] YAML 保存時に `pin` / `pinType` / `acceptPackageAgreements` / `acceptSourceAgreements` が脱落する（スキーマ不一致）: `src/GistGet/GistGet/Infrastructure/GitHubService.cs`
- [ ] `install` の `custom` が winget に正しく渡らない（`--custom` が付かず値だけ渡っている）: `src/GistGet/GistGet/GistGetService.cs`
- [ ] winget 実行失敗時に「エラー終了」にならない経路がある（exit code をプロセス終了コードへ反映しない/戻り値がない）: `src/GistGet/GistGet/GistGetService.cs`
- [ ] パススルー引数のクォート/エスケープが壊れやすい（`string.Join(" ", args)`）: `src/GistGet/GistGet/Infrastructure/WinGetPassthroughRunner.cs`

## 仕様（SPEC）との差分：コマンド

### sync

- [ ] `gistget sync [--url <gist-url>]` の処理フローが未実装: `docs/SPEC.ja.md` / `src/GistGet/GistGet/Presentation/CommandBuilder.cs`
- [ ] `--url` 指定時の「読み取り専用モード（Gistへ保存しない）」が未実装: `docs/SPEC.ja.md`
- [ ] 差分計算（同期マトリクス）/ uninstall・install・pin 同期（pin add/remove）が未実装: `docs/SPEC.ja.md`
- [ ] 冪等性・エラーハンドリング（複数パッケージ継続処理、最後にまとめて報告）が未実装: `docs/SPEC.ja.md`

### export / import

- [ ] `export`（ローカルのインストール済みパッケージを YAML 出力）が未実装: `docs/SPEC.ja.md`
- [ ] `import`（YAML ファイルを Gist にインポート）が未実装: `docs/SPEC.ja.md`

### install

- [ ] CLI が受けるオプションが SPEC/YAML スキーマの一部をカバーしていない（例: `--locale`, `--accept-*`, `--ignore-security-hash` など）: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`
- [ ] `InstallOptions` にはあるのに CLI が受けていないフィールドがある（`AcceptPackageAgreements` / `AcceptSourceAgreements` / `AllowHashMismatch` / `Locale` など）: `src/GistGet/GistGet/InstallOptions.cs`
- [ ] `packages.yaml` への永続化が「インストールオプション保存」の要件を満たしていない（保存しない/保存しても YAML 生成で落ちる）: `docs/SPEC.ja.md` / `src/GistGet/GistGet/Infrastructure/GitHubService.cs` / `src/GistGet/GistGet/GistGetService.cs`
- [ ] `custom` の winget パススルーが誤り（`--custom <value>` ではなく `<value>` のみ）: `src/GistGet/GistGet/GistGetService.cs`
- [ ] `acceptPackageAgreements` / `acceptSourceAgreements` を winget に渡す＆YAML に保存する実装が不足: `docs/SPEC.ja.md` / `src/GistGet/GistGet/GistGetService.cs`

### uninstall

- [ ] 「pin が存在すれば pin remove」を Gist 側の `pin` 有無でしか判断していない（ローカル pin の残存を取りこぼす可能性）: `docs/SPEC.ja.md` / `src/GistGet/GistGet/GistGetService.cs`
- [ ] winget 失敗時にエラー終了しない（Gist を更新しないだけで成功終了する可能性）: `docs/SPEC.ja.md` / `src/GistGet/GistGet/GistGetService.cs`

### upgrade

- [ ] upgrade 成功後の pin 追従（pin add --force + YAML 更新）が仕様どおりに動かない可能性（新バージョンの決定に “更新可能版” を使っており、upgrade 後のインストール済み版を確実に取得していない）: `docs/SPEC.ja.md` / `src/GistGet/GistGet/GistGetService.cs` / `src/GistGet/GistGet/Infrastructure/WinGetService.cs`
- [ ] ID 未指定時の passthrough は実装意図はあるが、引数再構成が不安定（`ParseResult.Tokens` 依存）: `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

### pin add / pin remove

- [ ] pin add/remove 自体は実装されているが、YAML 生成で `pin`/`pinType`/`version` が落ちるため Gist と同期できない: `src/GistGet/GistGet/Infrastructure/GitHubService.cs`

## 仕様（SPEC）との差分：YAML スキーマ

- [ ] `packages.yaml` は「パッケージIDをキーとするマップ」だが、保存時のフィールド網羅が不足（特に `pin` / `pinType` / `accept*`）: `docs/SPEC.ja.md` / `src/GistGet/GistGet/Infrastructure/GitHubService.cs`
- [ ] 仕様で列挙されているフィールドのうち、モデルはあるが CLI/保存が未対応のものがある: `docs/SPEC.ja.md` / `src/GistGet/GistGet/GistGetPackage.cs` / `src/GistGet/GistGet/Presentation/CommandBuilder.cs`

## 仕様（SPEC）自体の不整合（文書修正候補）

- [ ] `sync` 節で `gistget upgrade --id <id> --pin <version>` と記載があるが、`upgrade` 節は `--version` を定義している（`--pin` は未定義）: `docs/SPEC.ja.md`

## csproj / 依存関係 / リリース整合性

- [ ] リポジトリガイドライン（`net10.0-windows...`）と `TargetFramework` が不一致（現状 `net8.0-windows...`）: `src/GistGet/GistGet.csproj`
- [ ] `Microsoft.Identity.Client` が未使用に見える（依存整理 or 実装へ反映）: `src/GistGet/GistGet.csproj`
- [ ] `RootNamespace` が空（意図確認）: `src/GistGet/GistGet.csproj`
- [ ] Gist ファイル名/説明のデフォルト値が実装内で揺れている可能性（`packages.yaml` / `gistget-packages.yaml` 等、意図確認）: `src/GistGet/GistGet/Infrastructure/GitHubService.cs` / `src/GistGet/GistGet/GistGetService.cs`

## テスト（TDD前提の不足）

- [ ] 仕様の「同期マトリクス」「エラーハンドリング」「pin 追従」などの振る舞いがテストで固定されていない（追加が必要）: `docs/SPEC.ja.md`
