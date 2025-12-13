using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using GistGet.Infrastructure;

namespace GistGet;

/// <summary>
/// GistGetの中核サービスクラス。
/// GitHub Gistとの同期、WinGetコマンドの実行、認証管理を統合的に提供します。
/// </summary>
/// <param name="gitHubService">GitHub APIとの通信を担当するサービス</param>
/// <param name="consoleService">コンソール出力を担当するサービス</param>
/// <param name="credentialService">資格情報の永続化を担当するサービス</param>
/// <param name="passthroughRunner">WinGetコマンドのパススルー実行を担当するサービス</param>
/// <param name="winGetService">ローカルパッケージ情報の取得を担当するサービス</param>
public class GistGetService(
    IGitHubService gitHubService,
    IConsoleService consoleService,
    ICredentialService credentialService,
    IWinGetPassthroughRunner passthroughRunner,
    IWinGetService winGetService,
    IWinGetArgumentBuilder argumentBuilder) 
    : IGistGetService
{
    /// <summary>
    /// GitHubへのログイン処理を実行します。
    /// Device Flowによる認証を行い、取得した資格情報を保存します。
    /// </summary>
    public async Task AuthLoginAsync()
    {
        var credential = await gitHubService.LoginAsync();
        credentialService.SaveCredential(credential);
    }

    /// <summary>
    /// ログアウト処理を実行します。
    /// 保存されている資格情報を削除します。
    /// </summary>
    public void AuthLogout()
    {
        credentialService.DeleteCredential();
        consoleService.WriteInfo("Logged out");
    }

    /// <summary>
    /// 現在の認証状態を表示します。
    /// ログイン中の場合、ユーザー名、トークン情報、スコープを表示します。
    /// </summary>
    public async Task AuthStatusAsync()
    {
        if (credentialService.TryGetCredential(out var credential))
        {
             try
             {
                 var status = await gitHubService.GetTokenStatusAsync(credential.Token);

                 // セキュリティのため、トークンを部分的にマスクして表示
                 var tokenSafeDisplay = "**********";
                 if (!string.IsNullOrEmpty(credential.Token) && credential.Token.StartsWith("gho_"))
                 {
                     tokenSafeDisplay = "gho_**********";
                 }
                 else if (!string.IsNullOrEmpty(credential.Token) && credential.Token.Length > 4)
                 {
                     tokenSafeDisplay = credential.Token[..4] + "**********";
                 }

                 var scopesStr = string.Join(", ", status.Scopes.Select(s => $"'{s}'"));

                 // gh auth status 形式で認証情報を出力
                 consoleService.WriteInfo("github.com");
                 consoleService.WriteInfo($"  ✓ Logged in to github.com account {status.Username} (keyring)");
                 consoleService.WriteInfo("  - Active account: true");
                 consoleService.WriteInfo("  - Git operations protocol: https");
                 consoleService.WriteInfo($"  - Token: {tokenSafeDisplay}");
                 consoleService.WriteInfo($"  - Token scopes: {scopesStr}");
             }
             catch (Exception ex)
             {
                 consoleService.WriteInfo($"Failed to retrieve status from GitHub: {ex.Message}");
             }
        }
        else
        {
            consoleService.WriteInfo("You are not logged in.");
        }
    }

    /// <summary>
    /// パッケージをインストールし、Gistに保存します。
    /// 認証確認、Gist取得、バージョン解決、インストール実行、Pin設定、Gist更新を行います。
    /// </summary>
    /// <param name="options">インストールオプション</param>
    public async Task<int> InstallAndSaveAsync(InstallOptions options)
    {
        // ステップ1: 認証状態を確認し、未認証の場合はログインを促す
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        // ステップ2: Gistから既存のパッケージ一覧を取得
        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, Constants.DefaultGistFileName, Constants.DefaultGistDescription);
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        // ステップ3: Pinロジックとインストールバージョンの解決
        string? installVersion = options.Version;
        string? pinVersionToSet = null;
        // 既存のPinTypeを継承（CLI引数で未指定の場合）
        string? pinTypeToSet = existingPackage?.PinType;

        if (!string.IsNullOrEmpty(options.Version))
        {
            // CLIでバージョンが明示的に指定された場合
            installVersion = options.Version;

            if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
            {
                // Gistに既存のPinがある場合、インストールバージョンに更新
                pinVersionToSet = options.Version;
            }
        }
        else
        {
            // CLIでバージョン未指定の場合
            if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
            {
                // GistのPinバージョンを使用
                installVersion = existingPackage.Pin;
                pinVersionToSet = existingPackage.Pin;
            }
        }

        // ステップ4: WinGet installコマンドの引数を構築
        var installArgs = argumentBuilder.BuildInstallArgs(options);
        // Pinバージョン指定がある場合は上書き（CLIオプションのバージョンよりも優先される場合のロジックはビルダー外で制御が必要だが、
        // 現状のGistGetServiceの実装ではinstallArgsに--versionを追加するロジックが複雑。
        // ビルダーのBuildInstallArgsはoptions.Versionのみを見る。
        // ここでのロジック:
        // if (!string.IsNullOrEmpty(installVersion)) { installArgs.Add("--version"); ... }
        // 既存のコードでは `installVersion` 変数を使っている。これは options.Version または gist.Pin から来ている。
        // ビルダーを使うなら、options.Version を installVersion に書き換えたコピーを作るか、ビルダーに version オーバーライドを渡せるようにするか。
        // ここは一時的に InstallOptions を複製して Version を書き換えるのが一番副作用が少ない。
        
        if (installVersion != options.Version)
        {
            options = options with { Version = installVersion };
            installArgs = argumentBuilder.BuildInstallArgs(options);
        }



        // ステップ5: WinGet installを実行
        var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());
        if (exitCode != 0)
        {
            // インストール失敗時はGistを更新せずに終了コードを返す
            return exitCode;
        }

        // ステップ6: 必要に応じてWinGet pin addを実行
        if (!string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = argumentBuilder.BuildPinAddArgs(options.Id, pinVersionToSet, pinTypeToSet);
            await passthroughRunner.RunAsync(pinArgs);
        }


        // ステップ7: パッケージリストを更新してGistに保存
        var newPackagesList = existingPackages.Where(p => !string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        
        // CLI引数とGist状態をマージした新しいパッケージエントリを作成
        var versionToSave = pinVersionToSet;
        var packageToSave = new GistGetPackage
        {
            Id = options.Id,
            Version = versionToSave,
            Pin = pinVersionToSet,
            PinType = pinTypeToSet,
            
            // CLI引数のプロパティを保存
            Silent = options.Silent,
            Interactive = options.Interactive,
            Force = options.Force,
            Scope = options.Scope,
            Architecture = options.Architecture,
            Location = options.Location,
            Log = options.Log,
            Header = options.Header,
            Custom = options.Custom,
            Override = options.Override,
            AllowHashMismatch = options.AllowHashMismatch,
            SkipDependencies = options.SkipDependencies,
            InstallerType = options.InstallerType,
            Locale = options.Locale,
            AcceptPackageAgreements = options.AcceptPackageAgreements,
            AcceptSourceAgreements = options.AcceptSourceAgreements,
            
            // インストール直後なのでuninstallはfalse
            Uninstall = false 
        };

        newPackagesList.Add(packageToSave);
        
        await gitHubService.SavePackagesAsync(credential.Token, "", Constants.DefaultGistFileName, Constants.DefaultGistDescription, newPackagesList);
        return 0;
    }

    /// <summary>
    /// パッケージをアンインストールし、Gistを更新します。
    /// </summary>
    /// <param name="options">アンインストールオプション</param>
    public async Task<int> UninstallAndSaveAsync(UninstallOptions options)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, Constants.DefaultGistFileName, Constants.DefaultGistDescription);
        var targetPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        var uninstallArgs = argumentBuilder.BuildUninstallArgs(options);
        
        var exitCode = await passthroughRunner.RunAsync(uninstallArgs.ToArray());
        if (exitCode != 0)
        {
            return exitCode;
        }

        // ローカルのpinを常に削除（Gist側のpin有無に関係なく）
        // エラーは無視（元々pinがない場合もあるため）
        await passthroughRunner.RunAsync(new[] { "pin", "remove", "--id", options.Id });

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = targetPackage ?? new GistGetPackage { Id = options.Id };
        packageToSave.Uninstall = true;
        packageToSave.Pin = null;
        packageToSave.PinType = null;
        packageToSave.Version = null;

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(credential.Token, "", Constants.DefaultGistFileName, Constants.DefaultGistDescription, newPackages);
        return 0;
    }

    /// <summary>
    /// パッケージをアップグレードし、Gistを更新します。
    /// </summary>
    /// <param name="options">アップグレードオプション</param>
    public async Task<int> UpgradeAndSaveAsync(UpgradeOptions options)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var upgradeArgs = argumentBuilder.BuildUpgradeArgs(options);

        var exitCode = await passthroughRunner.RunAsync(upgradeArgs.ToArray());
        if (exitCode != 0)
        {
            return exitCode;
        }

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, Constants.DefaultGistFileName, Constants.DefaultGistDescription);
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        var resolvedVersion = options.Version;
        var hasPin = !string.IsNullOrEmpty(existingPackage?.Pin);
        var pinTypeToSet = existingPackage?.PinType;
        string? pinVersionToSet = null;

        if (hasPin && resolvedVersion == null)
        {
            var packageInfo = winGetService.FindById(new PackageId(options.Id));
            resolvedVersion = packageInfo?.Version.ToString();
        }

        if (hasPin)
        {
            pinVersionToSet = resolvedVersion ?? existingPackage!.Pin;
        }

        var shouldUpdateGist = existingPackage == null || existingPackage.Uninstall || hasPin;
        if (!shouldUpdateGist)
        {
            return 0;
        }

        if (hasPin && !string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = argumentBuilder.BuildPinAddArgs(options.Id, pinVersionToSet, pinTypeToSet, true);
            await passthroughRunner.RunAsync(pinArgs);
        }

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = existingPackage ?? new GistGetPackage { Id = options.Id };
        packageToSave.Uninstall = false;

        if (hasPin && !string.IsNullOrEmpty(pinVersionToSet))
        {
            packageToSave.Pin = pinVersionToSet;
            packageToSave.PinType = pinTypeToSet;
            packageToSave.Version = pinVersionToSet;
        }
        else
        {
            packageToSave.Pin = null;
            packageToSave.PinType = null;
            packageToSave.Version = null;
        }
        
        // Gist保存対象のオプションを反映
        packageToSave.Scope = options.Scope ?? packageToSave.Scope;
        packageToSave.Architecture = options.Architecture ?? packageToSave.Architecture;
        packageToSave.Location = options.Location ?? packageToSave.Location;
        packageToSave.Locale = options.Locale ?? packageToSave.Locale;
        packageToSave.Custom = options.Custom ?? packageToSave.Custom;
        packageToSave.Override = options.Override ?? packageToSave.Override;
        packageToSave.InstallerType = options.InstallerType ?? packageToSave.InstallerType;
        if (options.Force) packageToSave.Force = options.Force;
        if (options.AcceptPackageAgreements) packageToSave.AcceptPackageAgreements = options.AcceptPackageAgreements;
        if (options.AcceptSourceAgreements) packageToSave.AcceptSourceAgreements = options.AcceptSourceAgreements;
        if (options.AllowHashMismatch) packageToSave.AllowHashMismatch = options.AllowHashMismatch;
        if (options.SkipDependencies) packageToSave.SkipDependencies = options.SkipDependencies;

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(credential.Token, "", Constants.DefaultGistFileName, Constants.DefaultGistDescription, newPackages);
        return 0;
    }

    /// <summary>
    /// パッケージのPinを追加し、Gistを更新します。
    /// </summary>
    /// <param name="packageId">PinするパッケージのID</param>
    /// <param name="version">Pinするバージョン</param>
    /// <param name="pinType">Pinの種類（blocking, gating）。省略時は既存値を維持</param>
    /// <param name="force">既存のPinを上書きする場合はtrue（デフォルトはtrue）</param>
    public async Task PinAddAndSaveAsync(string packageId, string version, string? pinType = null, bool force = false)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, Constants.DefaultGistFileName, Constants.DefaultGistDescription);
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

        // CLIで指定されたpinTypeがあればそれを使用、なければ既存の値を継承
        var pinTypeToSet = pinType ?? existingPackage?.PinType;

        var pinArgs = argumentBuilder.BuildPinAddArgs(packageId, version, pinTypeToSet, force);

        var exitCode = await passthroughRunner.RunAsync(pinArgs.ToArray());
        if (exitCode != 0)
        {
            return;
        }

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = existingPackage ?? new GistGetPackage { Id = packageId };
        packageToSave.Uninstall = false;
        packageToSave.Pin = version;
        packageToSave.PinType = pinTypeToSet;
        packageToSave.Version = version;

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(credential.Token, "", Constants.DefaultGistFileName, Constants.DefaultGistDescription, newPackages);
    }

    /// <summary>
    /// パッケージのPinを削除し、Gistを更新します。
    /// </summary>
    /// <param name="packageId">Pin解除するパッケージのID</param>
    public async Task PinRemoveAndSaveAsync(string packageId)
    {
        // ステップ1: 認証状態を確認し、未認証の場合はログインを促す
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        // ステップ2: Gistから既存のパッケージ一覧を取得
        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, Constants.DefaultGistFileName, Constants.DefaultGistDescription);
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

        // ステップ3: WinGet pin removeを実行
        var pinArgs = new[] { "pin", "remove", "--id", packageId };
        var exitCode = await passthroughRunner.RunAsync(pinArgs);
        if (exitCode != 0)
        {
            // pin remove 失敗時はGistを更新せずに終了
            return;
        }

        // ステップ4: パッケージリストを更新してGistに保存
        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = existingPackage ?? new GistGetPackage { Id = packageId };
        // pinとpinType、versionを削除
        packageToSave.Pin = null;
        packageToSave.PinType = null;
        packageToSave.Version = null;

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(credential.Token, "", Constants.DefaultGistFileName, Constants.DefaultGistDescription, newPackages);
    }

    /// <summary>
    /// GistのpackagesとYAMLとローカル状態を同期します。
    /// 差分を検出し、インストール/アンインストール/pin設定を実行します。
    /// </summary>
    /// <param name="url">同期元の URL（省略時は認証ユーザーの Gist）</param>
    /// <param name="filePath">同期元のローカル YAML ファイルパス（URL より優先）</param>
    /// <returns>同期結果（インストール/アンインストール/失敗したパッケージ一覧）</returns>
    public async Task<SyncResult> SyncAsync(string? url = null, string? filePath = null)
    {
        var result = new SyncResult();
        
        IReadOnlyList<GistGetPackage> gistPackages;

        if (!string.IsNullOrEmpty(filePath))
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            var yaml = await File.ReadAllTextAsync(filePath);
            gistPackages = GistGetPackageSerializer.Deserialize(yaml);
        }
        else if (!string.IsNullOrEmpty(url))
        {
            // URL 指定時: HTTP でダウンロード（認証不要）
            gistPackages = await gitHubService.GetPackagesFromUrlAsync(url);
        }
        else
        {
            // URL 未指定時: 認証ユーザーの Gist を使用
            if (!credentialService.TryGetCredential(out var credential))
            {
                await AuthLoginAsync();
                if (!credentialService.TryGetCredential(out credential))
                {
                    throw new InvalidOperationException("Failed to retrieve credentials after login.");
                }
            }
            gistPackages = await gitHubService.GetPackagesAsync(credential.Token, Constants.DefaultGistFileName, Constants.DefaultGistDescription);
        }

        // ステップ3: ローカルのインストール済みパッケージを取得
        var localPackages = winGetService.GetAllInstalledPackages();
        var localPackageDict = localPackages.ToDictionary(
            p => p.Id.AsPrimitive(), 
            p => p, 
            StringComparer.OrdinalIgnoreCase);

        // ステップ4: Phase 1 - アンインストール（uninstall: trueのパッケージ）
        foreach (var gistPkg in gistPackages.Where(p => p.Uninstall))
        {
            if (localPackageDict.ContainsKey(gistPkg.Id))
            {
                try
                {
                    consoleService.WriteInfo($"[sync] Uninstalling {gistPkg.Id}...");
                    var uninstallArgs = new[] { "uninstall", "--id", gistPkg.Id };
                    var exitCode = await passthroughRunner.RunAsync(uninstallArgs);
                    if (exitCode == 0)
                    {
                        result.Uninstalled.Add(gistPkg);
                        consoleService.WriteInfo($"[sync] Uninstalled {gistPkg.Id}");

                        var pinRemoveArgs = new[] { "pin", "remove", "--id", gistPkg.Id };
                        consoleService.WriteInfo($"[sync] Removing pin for {gistPkg.Id}...");
                        var pinRemoveExitCode = await passthroughRunner.RunAsync(pinRemoveArgs);
                        if (pinRemoveExitCode == 0)
                        {
                            consoleService.WriteInfo($"[sync] Removed pin for {gistPkg.Id}");
                        }
                        // pinも削除
                    }
                    else
                    {
                        result.Failed.Add(gistPkg);
                        result.Errors.Add($"Failed to uninstall {gistPkg.Id}: exit code {exitCode}");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed.Add(gistPkg);
                    result.Errors.Add($"Failed to uninstall {gistPkg.Id}: {ex.Message}");
                }
            }
        }

        // ステップ5: Phase 2 - インストール（Gistにあり、ローカルにないパッケージ）
        foreach (var gistPkg in gistPackages.Where(p => !p.Uninstall))
        {
            if (!localPackageDict.ContainsKey(gistPkg.Id))
            {
                try
                {
                    consoleService.WriteInfo($"[sync] Installing {gistPkg.Id}...");
                    var installArgs = argumentBuilder.BuildInstallArgs(gistPkg);

                    var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());
                    if (exitCode == 0)
                    {
                        result.Installed.Add(gistPkg);
                        consoleService.WriteInfo($"[sync] Installed {gistPkg.Id}");
                        
                        // Pinがある場合はpin addを実行
                        if (!string.IsNullOrEmpty(gistPkg.Pin))
                        {
                            var pinArgs = argumentBuilder.BuildPinAddArgs(gistPkg.Id, gistPkg.Pin, gistPkg.PinType);
                            consoleService.WriteInfo($"[sync] Applying pin for {gistPkg.Id} ({gistPkg.Pin})...");
                            var pinExitCode = await passthroughRunner.RunAsync(pinArgs);
                            if (pinExitCode == 0)
                            {
                                consoleService.WriteInfo($"[sync] Applied pin for {gistPkg.Id}");
                            }
                        }
                    }
                    else
                    {
                        result.Failed.Add(gistPkg);
                        result.Errors.Add($"Failed to install {gistPkg.Id}: exit code {exitCode}");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed.Add(gistPkg);
                    result.Errors.Add($"Failed to install {gistPkg.Id}: {ex.Message}");
                }
            }
        }

        // ステップ6: Phase 3 - Pin同期（既にインストール済みのパッケージのpin設定を同期）
        foreach (var gistPkg in gistPackages.Where(p => !p.Uninstall))
        {
            if (localPackageDict.ContainsKey(gistPkg.Id))
            {
                try
                {
                    if (!string.IsNullOrEmpty(gistPkg.Pin))
                    {
                        // GistにPinがある場合はローカルにPin追加/更新
                        var pinArgs = argumentBuilder.BuildPinAddArgs(gistPkg.Id, gistPkg.Pin, gistPkg.PinType, true);
                        consoleService.WriteInfo($"[sync] Updating pin for {gistPkg.Id} to {gistPkg.Pin}...");
                        var exitCode = await passthroughRunner.RunAsync(pinArgs);
                        if (exitCode == 0)
                        {
                            result.PinUpdated.Add(gistPkg);
                            consoleService.WriteInfo($"[sync] Updated pin for {gistPkg.Id}");
                        }
                    }
                    else
                    {
                        // GistにPinがない場合はローカルのPinを削除（存在する場合）
                        // pin removeはエラーを無視（元々pinがない場合もあるため）
                        var pinRemoveArgs = new[] { "pin", "remove", "--id", gistPkg.Id };
                        consoleService.WriteInfo($"[sync] Removing pin for {gistPkg.Id}...");
                        var exitCode = await passthroughRunner.RunAsync(pinRemoveArgs);
                        if (exitCode == 0)
                        {
                            result.PinRemoved.Add(gistPkg);
                            consoleService.WriteInfo($"[sync] Removed pin for {gistPkg.Id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to sync pin for {gistPkg.Id}: {ex.Message}");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 指定されたコマンドをWinGetにそのままパススルーで実行します。
    /// </summary>
    /// <param name="command">WinGetコマンド（例: list, search, show）</param>
    /// <param name="args">コマンドに渡す引数</param>
    /// <returns>WinGetプロセスの終了コード</returns>
    public Task<int> RunPassthroughAsync(string command, string[] args)
    {
        var fullArgs = new List<string> { command };
        fullArgs.AddRange(args);
        return passthroughRunner.RunAsync(fullArgs.ToArray());
    }

    /// <summary>
    /// ローカルにインストールされているパッケージをYAML形式でエクスポートします。
    /// COMから取得可能な情報（ID）のみを出力します。
    /// </summary>
    /// <param name="outputPath">出力先ファイルパス（nullの場合は標準出力に出力）</param>
    /// <returns>エクスポートされたYAML文字列</returns>
    public async Task<string> ExportAsync(string? outputPath = null)
    {
        // ローカルのインストール済みパッケージを取得
        var installedPackages = winGetService.GetAllInstalledPackages();
        
        // GistGetPackage形式に変換（COMから取得可能な情報のみ）
        var packages = installedPackages.Select(p => new GistGetPackage
        {
            Id = p.Id.AsPrimitive()
        }).ToList();

        // YAMLにシリアライズ
        var yaml = GistGetPackageSerializer.Serialize(packages);

        if (!string.IsNullOrEmpty(outputPath))
        {
            // ファイルに出力
            await File.WriteAllTextAsync(outputPath, yaml);
            consoleService.WriteInfo($"Exported {packages.Count} packages to {outputPath}");
        }
        else
        {
            // 標準出力に出力
            Console.WriteLine(yaml);
        }

        return yaml;
    }

    /// <summary>
    /// YAMLファイルからパッケージ情報を読み込み、Gistに保存します。
    /// </summary>
    /// <param name="filePath">読み込むYAMLファイルのパス</param>
    public async Task ImportAsync(string filePath)
    {
        // 認証確認
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        // ファイルを読み込み
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var yaml = await File.ReadAllTextAsync(filePath);
        var packages = GistGetPackageSerializer.Deserialize(yaml);

        // Gistに保存
        await gitHubService.SavePackagesAsync(credential.Token, "", Constants.DefaultGistFileName, Constants.DefaultGistDescription, packages);
        consoleService.WriteInfo($"Imported {packages.Count} packages to Gist");
    }
}
