using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

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
    IWinGetService winGetService) 
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
    public async Task InstallAndSaveAsync(InstallOptions options)
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
        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "packages.yaml", "GistGet packages");
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
        var installArgs = new List<string> { "install", "--id", options.Id };
        if (!string.IsNullOrEmpty(installVersion))
        {
            installArgs.Add("--version");
            installArgs.Add(installVersion);
        }

        // オプションからフラグを追加
        if (options.Silent) installArgs.Add("--silent");
        if (options.Interactive) installArgs.Add("--interactive");
        if (options.Force) installArgs.Add("--force");
        if (options.AcceptPackageAgreements) installArgs.Add("--accept-package-agreements");
        if (options.AcceptSourceAgreements) installArgs.Add("--accept-source-agreements");
        if (options.AllowHashMismatch) installArgs.Add("--ignore-security-hash");
        if (options.SkipDependencies) installArgs.Add("--skip-dependencies");
        if (options.Scope != null) { installArgs.Add("--scope"); installArgs.Add(options.Scope); }
        if (options.Architecture != null) { installArgs.Add("--architecture"); installArgs.Add(options.Architecture); }
        if (options.Location != null) { installArgs.Add("--location"); installArgs.Add(options.Location); }
        if (options.Log != null) { installArgs.Add("--log"); installArgs.Add(options.Log); }
        if (options.Header != null) { installArgs.Add("--header"); installArgs.Add(options.Header); }
        if (options.Custom != null) { installArgs.Add("--custom"); installArgs.Add(options.Custom); }
        if (options.Override != null) { installArgs.Add("--override"); installArgs.Add(options.Override); }
        if (options.InstallerType != null) { installArgs.Add("--installer-type"); installArgs.Add(options.InstallerType); }
        if (options.Locale != null) { installArgs.Add("--locale"); installArgs.Add(options.Locale); }


        // ステップ5: WinGet installを実行
        var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());
        if (exitCode != 0)
        {
            // インストール失敗時はGistを更新せずに終了
            return;
        }

        // ステップ6: 必要に応じてWinGet pin addを実行
        if (!string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = new List<string> { "pin", "add", "--id", options.Id, "--version", pinVersionToSet };
            if (!string.IsNullOrEmpty(pinTypeToSet))
            {
                // pinType に応じたフラグを追加
                if (pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase))
                {
                    pinArgs.Add("--blocking");
                }
                else if (pinTypeToSet.Equals("gating", StringComparison.OrdinalIgnoreCase))
                {
                    pinArgs.Add("--gating");
                }
            }
            await passthroughRunner.RunAsync(pinArgs.ToArray());
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
            
            // インストール直後なのでuninstallはfalse
            Uninstall = false 
        };

        newPackagesList.Add(packageToSave);
        
        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackagesList);
    }

    /// <summary>
    /// パッケージをアンインストールし、Gistを更新します。
    /// </summary>
    /// <param name="packageId">アンインストールするパッケージのID</param>
    public async Task UninstallAndSaveAsync(string packageId)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "packages.yaml", "GistGet packages");
        var targetPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

        var uninstallArgs = new[] { "uninstall", "--id", packageId };
        var exitCode = await passthroughRunner.RunAsync(uninstallArgs);
        if (exitCode != 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(targetPackage?.Pin))
        {
            await passthroughRunner.RunAsync(new[] { "pin", "remove", "--id", packageId });
        }

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = targetPackage ?? new GistGetPackage { Id = packageId };
        packageToSave.Uninstall = true;
        packageToSave.Pin = null;
        packageToSave.PinType = null;
        packageToSave.Version = null;

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackages);
    }

    /// <summary>
    /// パッケージをアップグレードし、Gistを更新します。
    /// </summary>
    /// <param name="packageId">アップグレードするパッケージのID</param>
    /// <param name="version">アップグレード先のバージョン（省略可）</param>
    public async Task UpgradeAndSaveAsync(string packageId, string? version = null)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var upgradeArgs = new List<string> { "upgrade", "--id", packageId };
        if (!string.IsNullOrEmpty(version))
        {
            upgradeArgs.Add("--version");
            upgradeArgs.Add(version);
        }

        var exitCode = await passthroughRunner.RunAsync(upgradeArgs.ToArray());
        if (exitCode != 0)
        {
            return;
        }

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "packages.yaml", "GistGet packages");
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

        var resolvedVersion = version;
        var hasPin = !string.IsNullOrEmpty(existingPackage?.Pin);
        var pinTypeToSet = existingPackage?.PinType;
        string? pinVersionToSet = null;

        if (hasPin && resolvedVersion == null)
        {
            var packageInfo = winGetService.FindById(new PackageId(packageId));
            resolvedVersion = packageInfo?.UsableVersion?.ToString();
        }

        if (hasPin)
        {
            pinVersionToSet = resolvedVersion ?? existingPackage!.Pin;
        }

        var shouldUpdateGist = existingPackage == null || existingPackage.Uninstall || hasPin;
        if (!shouldUpdateGist)
        {
            return;
        }

        if (hasPin && !string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = new List<string> { "pin", "add", "--id", packageId, "--version", pinVersionToSet, "--force" };
            if (!string.IsNullOrEmpty(pinTypeToSet))
            {
                if (pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase))
                {
                    pinArgs.Add("--blocking");
                }
                else if (pinTypeToSet.Equals("gating", StringComparison.OrdinalIgnoreCase))
                {
                    pinArgs.Add("--gating");
                }
            }

            await passthroughRunner.RunAsync(pinArgs.ToArray());
        }

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = existingPackage ?? new GistGetPackage { Id = packageId };
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

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackages);
    }

    /// <summary>
    /// パッケージのPinを追加し、Gistを更新します。
    /// </summary>
    /// <param name="packageId">PinするパッケージのID</param>
    /// <param name="version">Pinするバージョン</param>
    public async Task PinAddAndSaveAsync(string packageId, string version)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "packages.yaml", "GistGet packages");
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

        var pinTypeToSet = existingPackage?.PinType;

        var pinArgs = new List<string> { "pin", "add", "--id", packageId, "--version", version, "--force" };
        if (!string.IsNullOrEmpty(pinTypeToSet))
        {
            if (pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase))
            {
                pinArgs.Add("--blocking");
            }
            else if (pinTypeToSet.Equals("gating", StringComparison.OrdinalIgnoreCase))
            {
                pinArgs.Add("--gating");
            }
        }

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

        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackages);
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
        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "packages.yaml", "GistGet packages");
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

        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackages);
    }

    /// <summary>
    /// GistのpackagesとYAMLとローカル状態を同期します。
    /// 差分を検出し、インストール/アンインストール/pin設定を実行します。
    /// </summary>
    /// <param name="url">同期元の URL（省略時は認証ユーザーの Gist）</param>
    /// <returns>同期結果（インストール/アンインストール/失敗したパッケージ一覧）</returns>
    public async Task<SyncResult> SyncAsync(string? url = null)
    {
        var result = new SyncResult();
        
        IReadOnlyList<GistGetPackage> gistPackages;

        if (!string.IsNullOrEmpty(url))
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
            gistPackages = await gitHubService.GetPackagesAsync(credential.Token, "packages.yaml", "GistGet packages");
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
                    var uninstallArgs = new[] { "uninstall", "--id", gistPkg.Id };
                    var exitCode = await passthroughRunner.RunAsync(uninstallArgs);
                    if (exitCode == 0)
                    {
                        result.Uninstalled.Add(gistPkg);
                        // pinも削除
                        await passthroughRunner.RunAsync(new[] { "pin", "remove", "--id", gistPkg.Id });
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
                    var installArgs = new List<string> { "install", "--id", gistPkg.Id };
                    
                    // Pinバージョンがある場合はそのバージョンをインストール
                    if (!string.IsNullOrEmpty(gistPkg.Pin))
                    {
                        installArgs.Add("--version");
                        installArgs.Add(gistPkg.Pin);
                    }
                    
                    // インストールオプションを追加
                    if (gistPkg.Silent) installArgs.Add("--silent");
                    if (gistPkg.Interactive) installArgs.Add("--interactive");
                    if (gistPkg.Force) installArgs.Add("--force");
                    if (gistPkg.AcceptPackageAgreements) installArgs.Add("--accept-package-agreements");
                    if (gistPkg.AcceptSourceAgreements) installArgs.Add("--accept-source-agreements");
                    if (gistPkg.AllowHashMismatch) installArgs.Add("--ignore-security-hash");
                    if (gistPkg.SkipDependencies) installArgs.Add("--skip-dependencies");
                    if (gistPkg.Scope != null) { installArgs.Add("--scope"); installArgs.Add(gistPkg.Scope); }
                    if (gistPkg.Architecture != null) { installArgs.Add("--architecture"); installArgs.Add(gistPkg.Architecture); }
                    if (gistPkg.Location != null) { installArgs.Add("--location"); installArgs.Add(gistPkg.Location); }
                    if (gistPkg.Log != null) { installArgs.Add("--log"); installArgs.Add(gistPkg.Log); }
                    if (gistPkg.Header != null) { installArgs.Add("--header"); installArgs.Add(gistPkg.Header); }
                    if (gistPkg.Custom != null) { installArgs.Add("--custom"); installArgs.Add(gistPkg.Custom); }
                    if (gistPkg.Override != null) { installArgs.Add("--override"); installArgs.Add(gistPkg.Override); }
                    if (gistPkg.InstallerType != null) { installArgs.Add("--installer-type"); installArgs.Add(gistPkg.InstallerType); }
                    if (gistPkg.Locale != null) { installArgs.Add("--locale"); installArgs.Add(gistPkg.Locale); }

                    var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());
                    if (exitCode == 0)
                    {
                        result.Installed.Add(gistPkg);
                        
                        // Pinがある場合はpin addを実行
                        if (!string.IsNullOrEmpty(gistPkg.Pin))
                        {
                            var pinArgs = new List<string> { "pin", "add", "--id", gistPkg.Id, "--version", gistPkg.Pin };
                            if (!string.IsNullOrEmpty(gistPkg.PinType))
                            {
                                if (gistPkg.PinType.Equals("blocking", StringComparison.OrdinalIgnoreCase))
                                {
                                    pinArgs.Add("--blocking");
                                }
                                else if (gistPkg.PinType.Equals("gating", StringComparison.OrdinalIgnoreCase))
                                {
                                    pinArgs.Add("--gating");
                                }
                            }
                            await passthroughRunner.RunAsync(pinArgs.ToArray());
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
                        var pinArgs = new List<string> { "pin", "add", "--id", gistPkg.Id, "--version", gistPkg.Pin, "--force" };
                        if (!string.IsNullOrEmpty(gistPkg.PinType))
                        {
                            if (gistPkg.PinType.Equals("blocking", StringComparison.OrdinalIgnoreCase))
                            {
                                pinArgs.Add("--blocking");
                            }
                            else if (gistPkg.PinType.Equals("gating", StringComparison.OrdinalIgnoreCase))
                            {
                                pinArgs.Add("--gating");
                            }
                        }
                        var exitCode = await passthroughRunner.RunAsync(pinArgs.ToArray());
                        if (exitCode == 0)
                        {
                            result.PinUpdated.Add(gistPkg);
                        }
                    }
                    else
                    {
                        // GistにPinがない場合はローカルのPinを削除（存在する場合）
                        // pin removeはエラーを無視（元々pinがない場合もあるため）
                        var pinRemoveArgs = new[] { "pin", "remove", "--id", gistPkg.Id };
                        var exitCode = await passthroughRunner.RunAsync(pinRemoveArgs);
                        if (exitCode == 0)
                        {
                            result.PinRemoved.Add(gistPkg);
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
}

