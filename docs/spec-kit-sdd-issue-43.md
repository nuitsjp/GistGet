# Spec Kitを使ったIssue 43のSDD実装手順

この手順書は、Spec Kitを使ってIssue 43「sync --dry-run の追加」をSpec-Driven Developmentで進めるための作業手順をまとめたものです。

対象Issue:

- https://github.com/nuitsjp/GistGet/issues/43

## 現在の前提

次の作業は実施済みです。

```powershell
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v0.8.12
specify init my-project
```

## Spec Kit CLIの確認

Windows PowerShellでは、`specify version`や`specify check`のバナー出力がCP932で文字化けまたは失敗することがあります。実行前にUTF-8を指定します。

```powershell
$env:PYTHONIOENCODING = 'utf-8'
$env:PYTHONUTF8 = '1'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

specify version
specify check
```

確認済みのCLI情報:

- Specify CLI: `0.8.12`
- Python: `3.12.11`
- Platform: `Windows`
- `specify check`: `Specify CLI is ready to use!`

## GistGet本体へSpec Kitを適用する

`specify init my-project`は初期試行用の別ディレクトリを作るコマンドです。Issue 43をGistGet本体でSDD実装する場合は、リポジトリルートでSpec Kitを初期化します。

既存ファイルへの影響を確認できるよう、実行前に作業ツリーがクリーンであることを確認します。

```powershell
git status --short
```

Codex向けのスキル形式で初期化する場合:

```powershell
specify init --here --integration codex --integration-options="--skills" --script ps
```

すでに`.specify`やエージェント設定が存在し、Spec Kitのテンプレートを更新したい場合だけ`--force`を使います。`--force`は`.specify/memory/constitution.md`を上書きする可能性があるため、事前に退避します。

```powershell
Copy-Item .specify\memory\constitution.md .specify\memory\constitution.backup.md
specify init --here --force --integration codex --integration-options="--skills" --script ps
Move-Item -Force .specify\memory\constitution.backup.md .specify\memory\constitution.md
```

## Constitutionを作成する

GistGetの開発ルールをSpec Kitに覚えさせます。既存の`AGENTS.md`と`.github/instructions/cs.test.instructions.md`を根拠に、次の内容でConstitutionを作成します。

```text
/speckit.constitution
GistGetはWindows Package Manager向けの.NET CLIです。すべての対話、仕様、計画、タスク、コミットメッセージ、PR説明は日本語で記述します。C# 14、net10.0-windows10.0.26100.0、System.CommandLine、Spectre.Console、xUnit、Moq、Shouldlyを使います。t-wada式TDDを必須とし、RED-GREEN-REFACTORを破ってはいけません。新しい振る舞いは失敗するテストを先に追加してから、通すための最小実装を行います。後方互換性、フォールバック、過度な抽象化は明示要求がない限り追加しません。既存のDI、命名、ファイル構成、リソース管理に合わせます。破壊的操作を含む機能では、実操作とプレビューを明確に分離し、テストで副作用がないことを検証します。
```

生成後、`.specify/memory/constitution.md`を読み、次を満たすように手で整えます。

- 仕様、計画、タスクは日本語で書く
- RED-GREEN-REFACTORを品質ゲートにする
- `sync --dry-run`ではwinget操作とGist保存を禁止する
- KISS/YAGNIを優先する

## Issue 43の仕様を作成する

Issue 43の目的は、`gistget sync --dry-run`で同期内容をプレビューし、実際の変更を行わないことです。

```text
/speckit.specify
Issue 43「sync --dry-run の追加」を実装する。syncコマンドに--dry-runフラグを追加し、Gist上または--url/--fileで指定されたGistGet.yamlとローカルのwinget状態を比較して、実行予定の操作を表示する。--dry-runではwinget install、winget uninstall、winget upgrade、winget pin、Gistへの保存を一切実行しない。表示対象はインストール予定、アンインストール予定、アップグレード予定、ピン追加または更新予定、ピン削除予定、更新不要の件数とする。アップグレード予定は、Gist側のVersionまたはPinとローカルVersionが異なる場合に検出する。出力はCommandBuilder側でSpectre.ConsoleのTableを使う。差分があっても終了コードは0とする。既存のsync、--url、--fileの動作は変えない。t-wada式TDDで、失敗するテストから実装する。
```

仕様作成後、次の観点で曖昧さを潰します。

```text
/speckit.clarify
```

確認する判断:

- 差分ありの`--dry-run`終了コードは`0`に固定する
- Gistへの書き込みは行わず、読み取りだけ許可する
- `--file`指定時もdry-run対象にする
- `--url`指定時もdry-run対象にする
- サービスのAPIは`SyncAsync(string? url = null, string? filePath = null, bool dryRun = false)`にする
- dry-runの表示用データは`SyncResult`を拡張して表現し、専用サービスメソッドは追加しない
- 実装初回ではCI向けの機械可読出力は作らない

要件品質を確認します。

```text
/speckit.checklist
Issue 43の仕様が、実操作なし、Gist保存なし、インストール予定、アンインストール予定、アップグレード予定、ピン変更予定、更新不要件数、終了コード、既存syncへの非干渉、TDD観点を検証可能な受け入れ条件として表現できているか確認する。
```

## 実装計画を作成する

GistGetの現行構造に合わせて、技術計画を作ります。

```text
/speckit.plan
実装対象は.NET CLIの既存syncコマンド。System.CommandLineで--dry-runフラグをOption<bool>として追加し、ArgumentArity.Zero相当で値引数は取らせない。IGistGetService.SyncAsyncをSyncAsync(string? url = null, string? filePath = null, bool dryRun = false)に拡張し、専用メソッドは追加しない。dry-runの戻り値はSyncResultを拡張して表現し、アップグレード予定と更新不要件数を保持する。dry-runテーブル表示は通常syncの結果表示と同じCommandBuilder側でIAnsiConsoleとSpectre.Console.Tableを使う。差分計算はGistGetService内の既存syncロジックから副作用のない部分を分離する。禁止するwinget操作はIWinGetPassthroughRunner.RunAsyncによるinstall、uninstall、upgrade、pinなどのプロセス実行であり、IWinGetService.GetAllInstalledPackages()とIWinGetService.GetPinnedPackages()による状態取得は許可する。Gist保存はIGitHubService.SavePackagesAsyncを呼ばないことで保証する。テストはxUnit、Moq、Shouldly、Spectre.Console.Testingを使う。RED-GREEN-REFACTORを厳守し、1つの失敗テストごとに最小実装で通してから次へ進む。過度な抽象化や将来用のJSON出力は追加しない。
```

計画では、最低限次のファイルが候補になります。

- `src/NuitsJp.GistGet/Presentation/CommandBuilder.cs`
- `src/NuitsJp.GistGet/IGistGetService.cs`
- `src/NuitsJp.GistGet/GistGetService.cs`
- `src/NuitsJp.GistGet/SyncResult.cs`
- `src/NuitsJp.GistGet/Resources/Messages.resx`
- `src/NuitsJp.GistGet/Resources/Messages.ja.resx`
- `src/NuitsJp.GistGet.Test/Presentation/CommandBuilderTests.cs`
- `src/NuitsJp.GistGet.Test/GistGetServiceTest.cs`

`SyncResult`には、dry-runで使うための最小項目を追加します。

- `UpgradeRequired`: アップグレード予定のパッケージ
- `UnchangedCount`: 変更不要の件数

`UnchangedCount`は、全Gistパッケージ数からインストール予定、アンインストール予定、アップグレード予定、ピン追加または更新予定、ピン削除予定を差し引いて導出します。通常syncの既存表示には影響させません。

## タスクを作成する

タスクは必ずテストを先に並べます。

```text
/speckit.tasks
```

生成された`tasks.md`を確認し、REDをため込まず、次のように1テストずつRED-GREEN-REFACTORの順序になっていなければ調整します。

1. `sync --dry-run`が`SyncAsync(null, null, true)`を呼ぶ失敗テストを追加し、対象テストのREDを確認する
2. `--dry-run`フラグを`Option<bool>`で追加し、ArgumentArity.Zero相当の値引数なしでGREENにする
3. `--dry-run`指定時に`IWinGetPassthroughRunner.RunAsync`が呼ばれない失敗テストを追加し、REDを確認する
4. dry-run分岐を追加し、`GetAllInstalledPackages()`と`GetPinnedPackages()`だけを許可してGREENにする
5. `--dry-run`指定時に`IGitHubService.SavePackagesAsync`が呼ばれない失敗テストを追加し、REDを確認する
6. Gist読み取りのみで結果を返す最小実装にしてGREENにする
7. インストール予定を検出する失敗テストを追加し、GREENにする
8. アンインストール予定を検出する失敗テストを追加し、GREENにする
9. アップグレード予定を検出する失敗テストを追加し、GREENにする
10. ピン追加、ピン更新、ピン削除予定を検出する失敗テストを追加し、GREENにする
11. 更新不要件数を表示する失敗テストを追加し、GREENにする
12. dry-run結果をCommandBuilder側のSpectre.Console.Tableで表示する失敗テストを追加し、GREENにする
13. 重複した差分計算を読みやすく整理する
14. `dotnet test`で全体確認する

実装前に、成果物間の矛盾を確認します。

```text
/speckit.analyze
Issue 43のspec、plan、tasksの間で、dry-runの副作用禁止、表示内容、終了コード、TDD順序、GistGet既存構造との整合に矛盾や抜けがないか確認する。
```

## RED-GREEN-REFACTORで実装する

実装は`/speckit.implement`に任せる前に、TDDの進め方を明示します。

```text
/speckit.implement
tasks.mdの順に実装する。各タスクでは必ずREDとして対象テストだけを実行して失敗を確認し、GREENとして最小実装で通し、REFACTORとして重複や命名だけを整える。複数のREDをため込まない。実装中に新しい要件やフォールバックを追加しない。dry-runではIWinGetPassthroughRunner.RunAsyncとIGitHubService.SavePackagesAsyncが呼ばれないことをMoqのTimes.Neverで検証する。IWinGetService.GetAllInstalledPackages()とIWinGetService.GetPinnedPackages()は状態取得として許可する。
```

手動で実行する場合の基本コマンド:

```powershell
dotnet test src/GistGet.slnx -c Debug
```

対象テストだけ確認する場合の例:

```powershell
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj -c Debug --filter "FullyQualifiedName~SyncCommand"
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj -c Debug --filter "FullyQualifiedName~SyncAsync"
```

最終確認では、現在のテストプロジェクトに対してカバレッジ付きで実行します。

```powershell
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults
```

## 受け入れ条件

Issue 43の実装完了条件は次の通りです。

- `gistget sync --dry-run`を実行できる
- `gistget sync --dry-run --file <path>`を実行できる
- `gistget sync --dry-run --url <url>`を実行できる
- dry-runでは`IWinGetPassthroughRunner.RunAsync`を呼ばない
- dry-runでは`IGitHubService.SavePackagesAsync`を呼ばない
- Gistまたはファイルの読み取り、ローカルwinget状態の取得は行う
- インストール予定、アンインストール予定、アップグレード予定、ピン変更予定、変更不要件数が表示される
- 差分があっても終了コードは`0`
- 通常の`sync`の既存テストが壊れていない

## コミット前確認

```powershell
git status --short
dotnet build src/GistGet.slnx -c Debug
dotnet test src/NuitsJp.GistGet.Test/NuitsJp.GistGet.Test.csproj -c Debug --collect:"XPlat Code Coverage" --results-directory TestResults
```

コミットメッセージ例:

```text
feat: syncにdry-runを追加
```

PR説明には、次を日本語で記載します。

- 何を変更したか
- なぜ必要か
- 実操作なしでプレビューできること
- 実行した検証コマンド
- Issue 43へのリンク
