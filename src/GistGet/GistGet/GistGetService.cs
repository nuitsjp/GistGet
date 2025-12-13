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
        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages");
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
        if (options.Custom != null) installArgs.Add(options.Custom);
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
                // blocking タイプの場合はフラグを追加
                if (pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase)) pinArgs.Add("--blocking");
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

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages");
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

        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages");
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
            if (!string.IsNullOrEmpty(pinTypeToSet) && pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase))
            {
                pinArgs.Add("--blocking");
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
    public Task PinAddAndSaveAsync(string packageId, string version)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// パッケージのPinを削除し、Gistを更新します。
    /// </summary>
    /// <param name="packageId">Pin解除するパッケージのID</param>
    public Task PinRemoveAndSaveAsync(string packageId)
    {
        throw new NotImplementedException();
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
