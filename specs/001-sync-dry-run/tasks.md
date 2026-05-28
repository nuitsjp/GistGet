# タスク: sync dry-run

**入力**: `specs/001-sync-dry-run/` の設計ドキュメント

**前提**: [plan.md](./plan.md)、[spec.md](./spec.md)、[research.md](./research.md)、[data-model.md](./data-model.md)、[contracts/sync-dry-run.md](./contracts/sync-dry-run.md)、[quickstart.md](./quickstart.md)

**テスト**: t-wada 式 TDD に従い、新しい振る舞いには必ず先に失敗するテストを追加する。各 RED は対象テストだけを実行して失敗を確認してから、最小実装で GREEN にする。

**構成**: ユーザーストーリーごとに独立して実装、テスト、デモできる単位へ分割する。

## 形式: `[ID] [P?] [Story] 説明`

- **[P]**: 別ファイルを扱い、依存関係がないため並列実行できる。
- **[Story]**: 対応するユーザーストーリー。ユーザーストーリーフェーズのタスクにのみ付ける。
- 説明には正確なファイルパスを含める。

## Phase 1: 準備（共有基盤）

**目的**: 既存の同期処理、テスト配置、表示責務を確認し、以降の RED を小さく書ける状態にする。

- [ ] T001 `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs` の `sync` コマンド定義と既存表示処理を確認する
- [ ] T002 `src/NuitsJp.GistGet/IGistGetService.cs`、`src/NuitsJp.GistGet/GistGetService.cs`、`src/NuitsJp.GistGet/SyncResult.cs` の既存同期 API と結果モデルを確認する
- [ ] T003 [P] `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` の `SyncCommand` 既存テストと実行コマンドを確認する
- [ ] T004 [P] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` の `SyncAsync` 既存テスト、モック、テストデータ作成パターンを確認する

---

## Phase 2: 基盤（全ストーリーの前提）

**目的**: dry-run を表す最小の契約と結果モデルを、最初の RED に必要な範囲だけ準備する。

**重要**: このフェーズはユーザーストーリー実装の土台であり、完了前に US1 の実装へ進まない。

- [ ] T005 `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` に `sync --dry-run` が値引数なしのフラグとして受理される失敗テストを追加する
- [ ] T006 `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests"` を実行し、T005 の RED を確認する
- [ ] T007 `src/NuitsJp.GistGet/IGistGetService.cs` の `SyncAsync` に `bool dryRun = false` を追加し、既存呼び出しを壊さない最小契約へ変更する
- [ ] T008 `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs` に `--dry-run` の `Option<bool>` を追加し、`SyncAsync(url, filePath, dryRun)` へ渡す
- [ ] T009 `src/NuitsJp.GistGet/GistGetService.cs` の `SyncAsync` シグネチャに `bool dryRun = false` を追加し、通常同期の既存処理は変えずにビルドを通す
- [ ] T010 `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests"` を実行し、T005 の GREEN を確認する

---

## Phase 3: ユーザーストーリー 1 - 同期前に変更内容を確認する (優先度: P1)

**目標**: 通常の同期元に対して `sync --dry-run` を実行し、実操作なしでインストール、アンインストール、アップグレード、ピン変更予定を確認できる。

**独立テスト**: 同期元とローカル状態に差分がある状態で `sync --dry-run` を実行し、予定分類が `SyncResult` に入り、`IWinGetPassthroughRunner.RunAsync` と Gist 保存が呼ばれないことを確認する。

### RED

- [ ] T011 [US1] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run で未導入パッケージを `Installed` に分類する失敗テストを追加する
- [ ] T012 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、T011 の RED を確認する
- [ ] T013 [US1] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run で削除対象パッケージを `Uninstalled` に分類し `IWinGetPassthroughRunner.RunAsync` を呼ばない失敗テストを追加する
- [ ] T014 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、T013 の RED を確認する
- [ ] T015 [US1] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run でバージョン差分をアップグレード予定に分類する失敗テストを追加する
- [ ] T016 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、T015 の RED を確認する
- [ ] T017 [US1] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run でピン追加、更新、削除予定を分類しピン操作を呼ばない失敗テストを追加する
- [ ] T018 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、T017 の RED を確認する

### GREEN

- [ ] T019 [US1] `src/NuitsJp.GistGet/SyncResult.cs` に `Upgraded`、`UnchangedCount`、`IsDryRun` を追加し、既存プロパティ名は維持する
- [ ] T020 [US1] `src/NuitsJp.GistGet/GistGetService.cs` に dry-run 分岐を追加し、同期元読み込み、導入済み一覧取得、ピン一覧取得だけを行う
- [ ] T021 [US1] `src/NuitsJp.GistGet/GistGetService.cs` に `GistGetPackage` とローカル状態の分類処理を追加し、インストール予定、アンインストール予定、アップグレード予定、ピン予定、更新不要件数を `SyncResult` に設定する
- [ ] T022 [US1] `src/NuitsJp.GistGet/GistGetService.cs` の dry-run 分岐で `IWinGetPassthroughRunner.RunAsync` と `IGitHubService.SavePackagesAsync` を呼ばないことを GREEN にする
- [ ] T023 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、US1 の GREEN を確認する

### 表示

- [ ] T024 [US1] `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` に dry-run 結果の操作種別、対象パッケージ、件数が表示される失敗テストを追加する
- [ ] T025 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests"` を実行し、T024 の RED を確認する
- [ ] T026 [US1] `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs` に dry-run 用の `Spectre.Console.Table` 表示を追加する
- [ ] T027 [US1] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests"` を実行し、T024 の GREEN を確認する

**チェックポイント**: US1 だけで `sync --dry-run` の主要価値をデモでき、実操作が 0 件であることをテストで確認できる。

---

## Phase 4: ユーザーストーリー 2 - 任意の同期元でもプレビューする (優先度: P2)

**目標**: `--url` または `--file` と `--dry-run` を同時に指定し、指定同期元に対するプレビューを実操作なしで確認できる。

**独立テスト**: `sync --dry-run --url <URL>` と `sync --dry-run --file <PATH>` が dry-run 指定を保持してサービスへ渡り、サービスは指定同期元を読み込んで分類する。

### RED

- [ ] T028 [US2] `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` に `sync --dry-run --url <URL>` が `SyncAsync(url, null, true)` を呼ぶ失敗テストを追加する
- [ ] T029 [US2] `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` に `sync --dry-run --file <PATH>` が `SyncAsync(null, filePath, true)` を呼ぶ失敗テストを追加する
- [ ] T030 [US2] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests"` を実行し、T028 と T029 の RED を確認する
- [ ] T031 [US2] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run と `filePath` 指定でローカル YAML を分類し保存しない失敗テストを追加する
- [ ] T032 [US2] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run と `url` 指定で URL 同期元を分類し保存しない失敗テストを追加する
- [ ] T033 [US2] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、T031 と T032 の RED を確認する

### GREEN

- [ ] T034 [US2] `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs` の dry-run 引き渡しを `--url` と `--file` の同時利用でも GREEN になるよう調整する
- [ ] T035 [US2] `src/NuitsJp.GistGet/GistGetService.cs` の dry-run 分岐で既存の `GistGetPackagesAsync(url, filePath)` を使い、通常同期元、URL、ローカルファイルの読み込み経路を共通化する
- [ ] T036 [US2] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SyncAsync"` を実行し、US2 の GREEN を確認する

**チェックポイント**: US2 完了時点で、3 種類の同期元に対する dry-run がすべて同じ分類結果を返せる。

---

## Phase 5: ユーザーストーリー 3 - 差分の有無を安定して判断する (優先度: P3)

**目標**: 差分あり、差分なし、空の同期元で、更新不要件数と成功扱いが安定して表示される。

**独立テスト**: 差分なしの `sync --dry-run` で更新不要件数が表示され、差分ありでも終了コードが `0` になる。

### RED

- [ ] T037 [US3] `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` に dry-run で差分がない同期元パッケージを `UnchangedCount` に数える失敗テストを追加する
- [ ] T038 [US3] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"` を実行し、T037 の RED を確認する
- [ ] T039 [US3] `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` に dry-run の差分あり結果でも終了コードが `0` で、更新不要件数が表示される失敗テストを追加する
- [ ] T040 [US3] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests"` を実行し、T039 の RED を確認する

### GREEN

- [ ] T041 [US3] `src/NuitsJp.GistGet/GistGetService.cs` の分類処理でどの予定にも該当しない同期元パッケージを `UnchangedCount` に加算する
- [ ] T042 [US3] `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs` の dry-run 表示で更新不要件数と成功扱いを表示する
- [ ] T043 [US3] `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SyncAsync"` を実行し、US3 の GREEN を確認する

**チェックポイント**: US3 完了時点で、差分あり、差分なし、空同期元の判断がユーザーに伝わる。

---

## Phase 6: REFACTOR（整理）

**目的**: すべての対象テストが GREEN の状態を保ち、既存構造に沿って最小限の整理を行う。

- [ ] T044 `src/NuitsJp.GistGet/GistGetService.cs` の dry-run 分類処理を既存同期処理と重複しすぎない小さな private メソッドへ整理する
- [ ] T045 `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs` の通常 sync 表示と dry-run 表示の条件分岐を読みやすく整理する
- [ ] T046 `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs` の dry-run テストデータ作成を既存テストのパターンに合わせて整理する
- [ ] T047 `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs` の dry-run 表示テストで重複する Arrange を必要最小限に整理する
- [ ] T048 `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --filter "FullyQualifiedName~CommandBuilderTests|FullyQualifiedName~SyncAsync"` を実行し、REFACTOR 後も GREEN であることを確認する

---

## Phase 7: 仕上げと検証

**目的**: ユーザー向け説明と最終検証を整え、通常同期への影響がないことを確認する。

- [ ] T049 [P] `src/NuitsJp.GistGet/Resources/Messages.resx` に `--dry-run` オプション説明と dry-run 表示に必要な文言を追加する
- [ ] T050 [P] `src/NuitsJp.GistGet/Resources/Messages.ja.resx` に `--dry-run` オプション説明と dry-run 表示に必要な日本語文言を追加する
- [ ] T051 `src/NuitsJp.GistGet/Resources/Messages.Designer.cs` を既存のリソース生成方法に従って更新する
- [ ] T052 [P] `README.md` の `sync` コマンド説明に `gistget sync --dry-run`、`--file`、`--url` の実行例を追加する
- [ ] T053 `dotnet build src/GistGet.slnx -c Debug` を実行してビルド成功を確認する
- [ ] T054 `dotnet test src/GistGet.Tests/GistGet.Tests.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults` を実行して全体 GREEN とカバレッジ収集を確認する
- [ ] T055 `dotnet run --project src/GistGet/GistGet.csproj -- sync --help` を実行し、`--dry-run` がヘルプに表示されることを確認する
- [ ] T056 `dotnet run --project src/GistGet/GistGet.csproj -- sync --dry-run --file specs/001-sync-dry-run/fixtures/GistGet.dryrun.yaml` を実行し、代表的な手動確認結果を記録する

---

## 依存関係と実行順序

- Phase 1 は最初に実行する。
- Phase 2 は全ユーザーストーリーの前提であり、US1 より前に完了する。
- US1 は MVP。`sync --dry-run` の主要価値と副作用禁止を最初に完成させる。
- US2 は US1 の dry-run 分類が動いてから、同期元指定の組み合わせを追加検証する。
- US3 は US1 の分類結果に `UnchangedCount` と成功表示を足すため、US1 の後に実行する。
- Phase 6 は US1、US2、US3 がすべて GREEN になってから実行する。
- Phase 7 は最後に実行する。

## 並列化の機会

- T003 と T004 は別テストファイルの確認なので並列化できる。
- T049 と T050 は別リソースファイルなので並列化できる。
- T052 は README のみを扱うため、リソース更新と並列化できる。
- 同じ `GistGetService.cs` または `CommandBuilder.cs` を編集する GREEN タスクは競合しやすいため並列化しない。
- RED タスクは同じファイルに集中するため、各 RED の失敗確認を終えてから次へ進む。

## 実装戦略

### MVP first

1. Phase 1 と Phase 2 を完了する。
2. US1 の RED を 1 つ追加して失敗確認する。
3. 対応する最小 GREEN を実装する。
4. US1 の副作用禁止と表示まで通して、`sync --dry-run` の主要価値を完成させる。

### 段階的な追加

1. US2 で `--url` と `--file` を dry-run と組み合わせる。
2. US3 で差分なし、更新不要件数、成功扱いを固める。
3. REFACTOR で重複と命名だけを整える。
4. 最終検証でビルド、全テスト、ヘルプ表示、代表的な手動確認を行う。

## 独立テスト基準

- **US1**: 通常の同期元で dry-run を実行したとき、予定分類が返り、実操作と Gist 保存が呼ばれない。
- **US2**: `--url` と `--file` の各同期元指定で dry-run を実行したとき、指定した同期元に基づく分類になり、実操作と Gist 保存が呼ばれない。
- **US3**: 差分ありでも終了コードが `0` で、差分なしでは更新不要件数が表示される。

## 注意

- 複数の RED を溜めず、対象テストの失敗を確認してから最小実装へ進む。
- dry-run では `IWinGetPassthroughRunner.RunAsync` と `IGitHubService.SavePackagesAsync` を呼ばないことを必ず Moq の `Times.Never` で検証する。
- 状態取得として `IWinGetService.GetAllInstalledPackages()` と `IWinGetService.GetPinnedPackages()` は許可する。
- 明示要求がない限り、JSON 出力、対話確認、追加フォールバックは実装しない。
