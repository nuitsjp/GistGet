// Core application service that orchestrates GitHub and WinGet operations.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GistGet.Infrastructure;
using GistGet.Resources;
using Octokit;

namespace GistGet;

/// <summary>
/// Implements the main workflows for syncing, exporting, importing, and package operations.
/// </summary>
/// <param name="gitHubService">GitHub API facade.</param>
/// <param name="consoleService">Console output service.</param>
/// <param name="credentialService">Credential storage service.</param>
/// <param name="passthroughRunner">WinGet passthrough runner.</param>
/// <param name="winGetService">WinGet manifest service.</param>
/// <param name="argumentBuilder">WinGet argument builder.</param>
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
    /// Authenticates via GitHub device flow and stores the credential.
    /// </summary>
    public async Task AuthLoginAsync()
    {
        var credential = await gitHubService.LoginAsync();
        credentialService.SaveCredential(credential);
    }

    /// <summary>
    /// Logs out and removes any stored credential.
    /// </summary>
    public void AuthLogout()
    {
        credentialService.DeleteCredential();
        consoleService.WriteInfo("Logged out");
    }

    /// <summary>
    /// Displays the current authentication status.
    /// </summary>
    public async Task AuthStatusAsync()
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            consoleService.WriteInfo("You are not logged in.");
            return;
        }

        try
        {
            var status = await gitHubService.GetTokenStatusAsync(credential.Token);

            var tokenSafeDisplay = "**********";
            if (!string.IsNullOrEmpty(credential.Token))
            {
                if (credential.Token.StartsWith("gho_", StringComparison.Ordinal))
                {
                    tokenSafeDisplay = "gho_**********";
                }
                else if (credential.Token.Length > 4)
                {
                    tokenSafeDisplay = credential.Token[..4] + "**********";
                }
            }

            var scopesStr = string.Join(", ", status.Scopes.Select(s => $"'{s}'"));

            consoleService.WriteInfo("github.com");
            consoleService.WriteInfo($"  ✓ Logged in to github.com account {status.Username} (keyring)");
            consoleService.WriteInfo("  - Active account: true");
            consoleService.WriteInfo("  - Git operations protocol: https");
            consoleService.WriteInfo($"  - Token: {tokenSafeDisplay}");
            consoleService.WriteInfo($"  - Token scopes: {scopesStr}");
        }
        catch (HttpRequestException ex)
        {
            consoleService.WriteInfo($"Failed to retrieve status from GitHub: {ex.Message}");
        }
        catch (ApiException ex)
        {
            consoleService.WriteInfo($"Failed to retrieve status from GitHub: {ex.Message}");
        }
    }

    /// <summary>
    /// Installs a package and persists it to the manifest.
    /// </summary>
    /// <param name="options">Install options.</param>
    /// <returns>Process exit code.</returns>
    public async Task<int> InstallAndSaveAsync(InstallOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        IReadOnlyList<GistGetPackage> existingPackages;
        using (consoleService.WriteProgress(Messages.FetchingFromGist))
        {
            existingPackages = await gitHubService.GetPackagesAsync(
                credential.Token,
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription);
        }

        var existingPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        var installVersion = options.Version;
        string? pinVersionToSet = null;
        var pinTypeToSet = existingPackage?.PinType;

        if (!string.IsNullOrEmpty(options.Version))
        {
            installVersion = options.Version;

            if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
            {
                pinVersionToSet = options.Version;
            }
        }
        else if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
        {
            installVersion = existingPackage.Pin;
            pinVersionToSet = existingPackage.Pin;
        }

        var installArgs = argumentBuilder.BuildInstallArgs(options);
        if (installVersion != options.Version)
        {
            options = options with { Version = installVersion };
            installArgs = argumentBuilder.BuildInstallArgs(options);
        }

        var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());

        // Check if the package is installed locally after winget install attempt.
        // This handles cases where winget returns non-zero exit code but the package
        // is already installed (e.g., "no upgrade available" scenario).
        var localPackage = winGetService.FindById(new PackageId(options.Id));
        if (localPackage == null)
        {
            // Package is not installed, so winget install truly failed
            return exitCode;
        }

        if (!string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = argumentBuilder.BuildPinAddArgs(options.Id, pinVersionToSet, pinTypeToSet);
            await passthroughRunner.RunAsync(pinArgs);
        }

        var newPackagesList = existingPackages
            .Where(p => !string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var versionToSave = pinVersionToSet;
        var packageToSave = new GistGetPackage
        {
            Id = options.Id,
            Version = versionToSave,
            Pin = pinVersionToSet,
            PinType = pinTypeToSet,
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
            Uninstall = false
        };

        newPackagesList.Add(packageToSave);

        using (consoleService.WriteProgress(Messages.SavingToGist))
        {
            await gitHubService.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                newPackagesList);
        }

        consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.InstallSuccess, options.Id));

        return 0;
    }

    /// <summary>
    /// Uninstalls a package and updates the manifest.
    /// </summary>
    /// <param name="options">Uninstall options.</param>
    /// <returns>Process exit code.</returns>
    public async Task<int> UninstallAndSaveAsync(UninstallOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        IReadOnlyList<GistGetPackage> existingPackages;
        using (consoleService.WriteProgress(Messages.FetchingFromGist))
        {
            existingPackages = await gitHubService.GetPackagesAsync(
                credential.Token,
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription);
        }

        var targetPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        // Check if the package is installed locally
        var localPackage = winGetService.FindById(new PackageId(options.Id));
        var isInstalledLocally = localPackage != null;

        // Only run winget uninstall if the package is installed locally
        if (isInstalledLocally)
        {
            var uninstallArgs = argumentBuilder.BuildUninstallArgs(options);
            var exitCode = await passthroughRunner.RunAsync(uninstallArgs.ToArray());
            if (exitCode != 0)
            {
                return exitCode;
            }

            // Remove pin only if package was installed locally
            await passthroughRunner.RunAsync(["pin", "remove", "--id", options.Id]);
        }

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = targetPackage ?? new GistGetPackage { Id = options.Id };
        packageToSave.Uninstall = true;
        packageToSave.Pin = null;
        packageToSave.PinType = null;
        packageToSave.Version = null;

        newPackages.Add(packageToSave);

        using (consoleService.WriteProgress(Messages.SavingToGist))
        {
            await gitHubService.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                newPackages);
        }

        consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.UninstallSuccess, options.Id));

        return 0;
    }

    /// <summary>
    /// Upgrades a package and updates the manifest.
    /// </summary>
    /// <param name="options">Upgrade options.</param>
    /// <returns>Process exit code.</returns>
    public async Task<int> UpgradeAndSaveAsync(UpgradeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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

        IReadOnlyList<GistGetPackage> existingPackages;
        using (consoleService.WriteProgress(Messages.FetchingFromGist))
        {
            existingPackages = await gitHubService.GetPackagesAsync(
                credential.Token,
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription);
        }

        var existingPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

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
            consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.UpgradeSuccess, options.Id));
            return 0;
        }

        if (hasPin && !string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = argumentBuilder.BuildPinAddArgs(options.Id, pinVersionToSet, pinTypeToSet, force: true);
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

        packageToSave.Scope = options.Scope ?? packageToSave.Scope;
        packageToSave.Architecture = options.Architecture ?? packageToSave.Architecture;
        packageToSave.Location = options.Location ?? packageToSave.Location;
        packageToSave.Locale = options.Locale ?? packageToSave.Locale;
        packageToSave.Custom = options.Custom ?? packageToSave.Custom;
        packageToSave.Override = options.Override ?? packageToSave.Override;
        packageToSave.InstallerType = options.InstallerType ?? packageToSave.InstallerType;
        packageToSave.Header = options.Header ?? packageToSave.Header;
        if (options.Force)
        {
            packageToSave.Force = options.Force;
        }

        if (options.AcceptPackageAgreements)
        {
            packageToSave.AcceptPackageAgreements = options.AcceptPackageAgreements;
        }

        if (options.AcceptSourceAgreements)
        {
            packageToSave.AcceptSourceAgreements = options.AcceptSourceAgreements;
        }

        if (options.AllowHashMismatch)
        {
            packageToSave.AllowHashMismatch = options.AllowHashMismatch;
        }

        if (options.SkipDependencies)
        {
            packageToSave.SkipDependencies = options.SkipDependencies;
        }

        newPackages.Add(packageToSave);

        using (consoleService.WriteProgress(Messages.SavingToGist))
        {
            await gitHubService.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                newPackages);
        }

        consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.UpgradeAndSaveSuccess, options.Id));

        return 0;
    }

    /// <summary>
    /// Adds a pin and persists it to the manifest.
    /// </summary>
    /// <param name="packageId">Package identifier to pin.</param>
    /// <param name="version">Version to pin.</param>
    /// <param name="pinType">Optional pin type.</param>
    /// <param name="force">Whether to force pinning.</param>
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

        IReadOnlyList<GistGetPackage> existingPackages;
        using (consoleService.WriteProgress(Messages.FetchingFromGist))
        {
            existingPackages = await gitHubService.GetPackagesAsync(
                credential.Token,
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription);
        }

        var existingPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

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

        using (consoleService.WriteProgress(Messages.SavingPinToGist))
        {
            await gitHubService.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                newPackages);
        }

        consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.PinAddSuccess, packageId));
    }

    /// <summary>
    /// Removes a pin and updates the manifest.
    /// </summary>
    /// <param name="packageId">Package identifier to unpin.</param>
    public async Task PinRemoveAndSaveAsync(string packageId)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        IReadOnlyList<GistGetPackage> existingPackages;
        using (consoleService.WriteProgress(Messages.FetchingFromGist))
        {
            existingPackages = await gitHubService.GetPackagesAsync(
                credential.Token,
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription);
        }

        var existingPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

        var pinArgs = new[] { "pin", "remove", "--id", packageId };
        var exitCode = await passthroughRunner.RunAsync(pinArgs);
        if (exitCode != 0)
        {
            return;
        }

        var newPackages = existingPackages
            .Where(p => !string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var packageToSave = existingPackage ?? new GistGetPackage { Id = packageId };
        packageToSave.Pin = null;
        packageToSave.PinType = null;
        packageToSave.Version = null;

        newPackages.Add(packageToSave);

        using (consoleService.WriteProgress(Messages.SavingPinToGist))
        {
            await gitHubService.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                newPackages);
        }

        consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.PinRemoveSuccess, packageId));
    }

    /// <summary>
    /// Synchronizes the manifest with local state.
    /// </summary>
    /// <param name="url">Optional Gist URL to sync from.</param>
    /// <param name="filePath">Optional local YAML path to sync from.</param>
    /// <returns>Sync result.</returns>
    public async Task<SyncResult> SyncAsync(string? url = null, string? filePath = null)
    {
        var result = new SyncResult();

        consoleService.WriteInfo(Messages.SyncStarting);

        IReadOnlyList<GistGetPackage> gistPackages;
        if (!string.IsNullOrEmpty(filePath))
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            using (consoleService.WriteProgress(Messages.LoadingFromFile))
            {
                var yaml = await File.ReadAllTextAsync(filePath);
                gistPackages = GistGetPackageSerializer.Deserialize(yaml);
            }
        }
        else if (!string.IsNullOrEmpty(url))
        {
            using (consoleService.WriteProgress(Messages.FetchingFromUrl))
            {
                gistPackages = await gitHubService.GetPackagesFromUrlAsync(url);
            }
        }
        else
        {
            if (!credentialService.TryGetCredential(out var credential))
            {
                await AuthLoginAsync();
                if (!credentialService.TryGetCredential(out credential))
                {
                    throw new InvalidOperationException("Failed to retrieve credentials after login.");
                }
            }

            using (consoleService.WriteProgress(Messages.FetchingFromGist))
            {
                gistPackages = await gitHubService.GetPackagesAsync(
                    credential.Token,
                    Constants.DefaultGistFileName,
                    Constants.DefaultGistDescription);
            }
        }

        var localPackages = winGetService.GetAllInstalledPackages();
        var localPackageDict = localPackages.ToDictionary(
            p => p.Id.AsPrimitive(),
            p => p,
            StringComparer.OrdinalIgnoreCase);

        foreach (var gistPkg in gistPackages.Where(p => p.Uninstall))
        {
            if (!localPackageDict.ContainsKey(gistPkg.Id))
            {
                continue;
            }

            try
            {
                consoleService.WriteInfo($"[sync] Uninstalling {gistPkg.Id}...");
                var uninstallArgs = new[] { "uninstall", "--id", gistPkg.Id };
                var exitCode = await passthroughRunner.RunAsync(uninstallArgs);
                if (exitCode == 0)
                {
                    result.Uninstalled.Add(gistPkg);
                    consoleService.WriteInfo($"[sync] Removing pin for {gistPkg.Id}...");
                    await passthroughRunner.RunAsync(["pin", "remove", "--id", gistPkg.Id]);
                }
                else
                {
                    result.Failed.Add(gistPkg);
                    result.Errors.Add($"Failed to uninstall {gistPkg.Id}: exit code {exitCode.ToString(CultureInfo.InvariantCulture)}");
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
            {
                HandleSyncOperationFailure(result, gistPkg, "uninstall", ex);
            }
        }

        foreach (var gistPkg in gistPackages.Where(p => !p.Uninstall))
        {
            if (localPackageDict.ContainsKey(gistPkg.Id))
            {
                continue;
            }

            try
            {
                var installArgs = argumentBuilder.BuildInstallArgs(gistPkg);

                consoleService.WriteInfo($"[sync] Installing {gistPkg.Id}...");
                var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());
                if (exitCode == 0)
                {
                    result.Installed.Add(gistPkg);

                    if (!string.IsNullOrEmpty(gistPkg.Pin))
                    {
                        var pinArgs = argumentBuilder.BuildPinAddArgs(gistPkg.Id, gistPkg.Pin, gistPkg.PinType);
                        consoleService.WriteInfo($"[sync] Pinning {gistPkg.Id} to {gistPkg.Pin}...");
                        await passthroughRunner.RunAsync(pinArgs);
                    }
                }
                else
                {
                    result.Failed.Add(gistPkg);
                    result.Errors.Add($"Failed to install {gistPkg.Id}: exit code {exitCode.ToString(CultureInfo.InvariantCulture)}");
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
            {
                HandleSyncOperationFailure(result, gistPkg, "install", ex);
            }
        }

        foreach (var gistPkg in gistPackages.Where(p => !p.Uninstall))
        {
            if (!localPackageDict.ContainsKey(gistPkg.Id))
            {
                continue;
            }

            try
            {
                if (!string.IsNullOrEmpty(gistPkg.Pin))
                {
                    var pinArgs = argumentBuilder.BuildPinAddArgs(gistPkg.Id, gistPkg.Pin, gistPkg.PinType, true);
                    consoleService.WriteInfo($"[sync] Pinning {gistPkg.Id} to {gistPkg.Pin}...");
                    var exitCode = await passthroughRunner.RunAsync(pinArgs);
                    if (exitCode == 0)
                    {
                        result.PinUpdated.Add(gistPkg);
                    }
                }
                else
                {
                    var pinRemoveArgs = new[] { "pin", "remove", "--id", gistPkg.Id };
                    consoleService.WriteInfo($"[sync] Removing pin for {gistPkg.Id}...");
                    var exitCode = await passthroughRunner.RunAsync(pinRemoveArgs);
                    if (exitCode == 0)
                    {
                        result.PinRemoved.Add(gistPkg);
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
            {
                HandleSyncPinFailure(result, gistPkg, ex);
            }
        }

        consoleService.WriteSuccess(Messages.SyncCompleted);
        consoleService.WriteInfo(string.Format(CultureInfo.CurrentCulture, Messages.SyncSummary, result.Installed.Count, result.Uninstalled.Count, result.Failed.Count));

        return result;
    }

    /// <summary>
    /// Runs a WinGet command without syncing.
    /// </summary>
    /// <param name="command">WinGet command.</param>
    /// <param name="args">Arguments to pass.</param>
    /// <returns>Process exit code.</returns>
    public Task<int> RunPassthroughAsync(string command, string[] args)
    {
        var fullArgs = new List<string> { command };
        fullArgs.AddRange(args);
        return passthroughRunner.RunAsync(fullArgs.ToArray());
    }

    /// <summary>
    /// Exports installed packages to YAML.
    /// </summary>
    /// <param name="outputPath">Optional output file path.</param>
    /// <returns>Serialized YAML content.</returns>
    public async Task<string> ExportAsync(string? outputPath = null)
    {
        IReadOnlyList<WinGetPackage> installedPackages;
        using (consoleService.WriteProgress(Messages.FetchingInstalledPackages))
        {
            installedPackages = winGetService.GetAllInstalledPackages();
        }

        var packages = installedPackages.Select(p => new GistGetPackage
        {
            Id = p.Id.AsPrimitive(),
        }).ToList();

        var yaml = GistGetPackageSerializer.Serialize(packages);

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, yaml);
            consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.ExportSuccess, packages.Count, outputPath));
        }
        else
        {
            Console.WriteLine(yaml);
        }

        return yaml;
    }

    /// <summary>
    /// Imports package definitions from a YAML file.
    /// </summary>
    /// <param name="filePath">YAML file path.</param>
    public async Task ImportAsync(string filePath)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var yaml = await File.ReadAllTextAsync(filePath);
        var packages = GistGetPackageSerializer.Deserialize(yaml);

        using (consoleService.WriteProgress(Messages.SavingToGist))
        {
            await gitHubService.SavePackagesAsync(
                credential.Token,
                "",
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription,
                packages);
        }

        consoleService.WriteSuccess(string.Format(CultureInfo.CurrentCulture, Messages.ImportSuccess, packages.Count));
    }

    /// <summary>
    /// Handles failures during sync operations (install/uninstall).
    /// Defensive: captures process failures, I/O errors, and invalid operations.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static void HandleSyncOperationFailure(SyncResult result, GistGetPackage package, string operation, Exception ex)
    {
        result.Failed.Add(package);
        result.Errors.Add($"Failed to {operation} {package.Id}: {ex.Message}");
    }

    /// <summary>
    /// Handles failures during pin sync operations.
    /// Defensive: captures process failures, I/O errors, and invalid operations.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static void HandleSyncPinFailure(SyncResult result, GistGetPackage package, Exception ex)
    {
        result.Errors.Add($"Failed to sync pin for {package.Id}: {ex.Message}");
    }
}
