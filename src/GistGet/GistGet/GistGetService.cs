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
public class GistGetService(
    IGitHubService gitHubService,
    IConsoleService consoleService,
    ICredentialService credentialService,
    IWinGetPassthroughRunner passthroughRunner) 
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
    /// <param name="package">インストールするパッケージの情報</param>
    public async Task InstallAndSaveAsync(GistGetPackage package)
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
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, package.Id, StringComparison.OrdinalIgnoreCase));

        // ステップ3: Pinロジックとインストールバージョンの解決
        string? installVersion = package.Version;
        string? pinVersionToSet = null;
        // 既存のPinTypeを継承（CLI引数で未指定の場合）
        string? pinTypeToSet = existingPackage?.PinType;

        if (!string.IsNullOrEmpty(package.Version))
        {
            // CLIでバージョンが明示的に指定された場合
            installVersion = package.Version;

            if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
            {
                // Gistに既存のPinがある場合、インストールバージョンに更新
                pinVersionToSet = package.Version;
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
        var installArgs = new List<string> { "install", "--id", package.Id };
        if (!string.IsNullOrEmpty(installVersion))
        {
            installArgs.Add("--version");
            installArgs.Add(installVersion);
        }

        // パッケージオブジェクトからオプションフラグを追加
        if (package.Silent) installArgs.Add("--silent");
        if (package.Interactive) installArgs.Add("--interactive");
        if (package.Force) installArgs.Add("--force");
        if (package.AcceptPackageAgreements) installArgs.Add("--accept-package-agreements");
        if (package.AcceptSourceAgreements) installArgs.Add("--accept-source-agreements");
        if (package.Scope != null) { installArgs.Add("--scope"); installArgs.Add(package.Scope); }
        if (package.Architecture != null) { installArgs.Add("--architecture"); installArgs.Add(package.Architecture); }
        if (package.Location != null) { installArgs.Add("--location"); installArgs.Add(package.Location); }
        if (package.Log != null) { installArgs.Add("--log"); installArgs.Add(package.Log); }
        if (package.Header != null) { installArgs.Add("--header"); installArgs.Add(package.Header); }
        if (package.Custom != null) installArgs.Add(package.Custom);
        if (package.Override != null) { installArgs.Add("--override"); installArgs.Add(package.Override); }


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
            var pinArgs = new List<string> { "pin", "add", "--id", package.Id, "--version", pinVersionToSet };
            if (!string.IsNullOrEmpty(pinTypeToSet))
            {
                // blocking タイプの場合はフラグを追加
                if (pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase)) pinArgs.Add("--blocking");
            }
            await passthroughRunner.RunAsync(pinArgs.ToArray());
        }


        // ステップ7: パッケージリストを更新してGistに保存
        var newPackagesList = existingPackages.Where(p => !string.Equals(p.Id, package.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        
        // CLI引数とGist状態をマージした新しいパッケージエントリを作成
        var packageToSave = new GistGetPackage
        {
            Id = package.Id,
            Version = installVersion,
            Pin = pinVersionToSet,
            PinType = pinTypeToSet,
            
            // CLI引数のプロパティを保存
            Silent = package.Silent,
            Interactive = package.Interactive,
            Force = package.Force,
            Scope = package.Scope,
            Architecture = package.Architecture,
            Location = package.Location,
            Log = package.Log,
            Header = package.Header,
            Custom = package.Custom,
            Override = package.Override,
            AllowHashMismatch = package.AllowHashMismatch,
            SkipDependencies = package.SkipDependencies,
            InstallerType = package.InstallerType,
            
            // インストール直後なのでuninstallはfalse
            Uninstall = false 
        };
        
        // バージョンが未設定の場合、インストールしたバージョンを設定
        if (string.IsNullOrEmpty(packageToSave.Version) && !string.IsNullOrEmpty(installVersion))
        {
             packageToSave.Version = installVersion;
        }

        newPackagesList.Add(packageToSave);
        
        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackagesList);
    }

    /// <summary>
    /// パッケージをアンインストールし、Gistを更新します。
    /// </summary>
    /// <param name="packageId">アンインストールするパッケージのID</param>
    public Task UninstallAndSaveAsync(string packageId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// パッケージをアップグレードし、Gistを更新します。
    /// </summary>
    /// <param name="packageId">アップグレードするパッケージのID</param>
    /// <param name="version">アップグレード先のバージョン（省略可）</param>
    public Task UpgradeAndSaveAsync(string packageId, string? version = null)
    {
        throw new NotImplementedException();
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