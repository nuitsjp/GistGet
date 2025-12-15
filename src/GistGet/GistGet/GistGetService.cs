// Core application service that orchestrates GitHub and WinGet operations.

using System;
using System.Collections.Generic;
using System.Linq;
using GistGet.Infrastructure;

namespace GistGet;

/// <summary>
/// Implements the main workflows for syncing, exporting, importing, and package operations.
/// </summary>
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
            if (!string.IsNullOrEmpty(credential.Token) && credential.Token.StartsWith("gho_"))
            {
                tokenSafeDisplay = "gho_**********";
            }
            else if (!string.IsNullOrEmpty(credential.Token) && credential.Token.Length > 4)
            {
                tokenSafeDisplay = credential.Token[..4] + "**********";
            }

            var scopesStr = string.Join(", ", status.Scopes.Select(s => $"'{s}'"));

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

    /// <summary>
    /// Installs a package and persists it to the manifest.
    /// </summary>
    public async Task<int> InstallAndSaveAsync(InstallOptions options)
    {
        if (!credentialService.TryGetCredential(out var credential))
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential(out credential))
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        var existingPackages = await gitHubService.GetPackagesAsync(
            credential.Token,
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription);

        var existingPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        string? installVersion = options.Version;
        string? pinVersionToSet = null;
        string? pinTypeToSet = existingPackage?.PinType;

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
        if (exitCode != 0)
        {
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
            Uninstall = false,
        };

        newPackagesList.Add(packageToSave);

        await gitHubService.SavePackagesAsync(
            credential.Token,
            "",
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription,
            newPackagesList);

        return 0;
    }

    /// <summary>
    /// Uninstalls a package and updates the manifest.
    /// </summary>
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

        var existingPackages = await gitHubService.GetPackagesAsync(
            credential.Token,
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription);

        var targetPackage = existingPackages.FirstOrDefault(p =>
            string.Equals(p.Id, options.Id, StringComparison.OrdinalIgnoreCase));

        var uninstallArgs = argumentBuilder.BuildUninstallArgs(options);

        var exitCode = await passthroughRunner.RunAsync(uninstallArgs.ToArray());
        if (exitCode != 0)
        {
            return exitCode;
        }

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

        await gitHubService.SavePackagesAsync(
            credential.Token,
            "",
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription,
            newPackages);

        return 0;
    }

    /// <summary>
    /// Upgrades a package and updates the manifest.
    /// </summary>
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

        var existingPackages = await gitHubService.GetPackagesAsync(
            credential.Token,
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription);

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

        packageToSave.Scope = options.Scope ?? packageToSave.Scope;
        packageToSave.Architecture = options.Architecture ?? packageToSave.Architecture;
        packageToSave.Location = options.Location ?? packageToSave.Location;
        packageToSave.Locale = options.Locale ?? packageToSave.Locale;
        packageToSave.Custom = options.Custom ?? packageToSave.Custom;
        packageToSave.Override = options.Override ?? packageToSave.Override;
        packageToSave.InstallerType = options.InstallerType ?? packageToSave.InstallerType;
        packageToSave.Header = options.Header ?? packageToSave.Header;
        if (options.Force) packageToSave.Force = options.Force;
        if (options.AcceptPackageAgreements) packageToSave.AcceptPackageAgreements = options.AcceptPackageAgreements;
        if (options.AcceptSourceAgreements) packageToSave.AcceptSourceAgreements = options.AcceptSourceAgreements;
        if (options.AllowHashMismatch) packageToSave.AllowHashMismatch = options.AllowHashMismatch;
        if (options.SkipDependencies) packageToSave.SkipDependencies = options.SkipDependencies;

        newPackages.Add(packageToSave);

        await gitHubService.SavePackagesAsync(
            credential.Token,
            "",
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription,
            newPackages);

        return 0;
    }

    /// <summary>
    /// Adds a pin and persists it to the manifest.
    /// </summary>
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

        var existingPackages = await gitHubService.GetPackagesAsync(
            credential.Token,
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription);

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

        await gitHubService.SavePackagesAsync(
            credential.Token,
            "",
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription,
            newPackages);
    }

    /// <summary>
    /// Removes a pin and updates the manifest.
    /// </summary>
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

        var existingPackages = await gitHubService.GetPackagesAsync(
            credential.Token,
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription);

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

        await gitHubService.SavePackagesAsync(
            credential.Token,
            "",
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription,
            newPackages);
    }

    /// <summary>
    /// Synchronizes the manifest with local state.
    /// </summary>
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
            gistPackages = await gitHubService.GetPackagesFromUrlAsync(url);
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

            gistPackages = await gitHubService.GetPackagesAsync(
                credential.Token,
                Constants.DefaultGistFileName,
                Constants.DefaultGistDescription);
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
                    result.Errors.Add($"Failed to install {gistPkg.Id}: exit code {exitCode}");
                }
            }
            catch (Exception ex)
            {
                result.Failed.Add(gistPkg);
                result.Errors.Add($"Failed to install {gistPkg.Id}: {ex.Message}");
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
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to sync pin for {gistPkg.Id}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Runs a WinGet command without syncing.
    /// </summary>
    public Task<int> RunPassthroughAsync(string command, string[] args)
    {
        var fullArgs = new List<string> { command };
        fullArgs.AddRange(args);
        return passthroughRunner.RunAsync(fullArgs.ToArray());
    }

    /// <summary>
    /// Exports installed packages to YAML.
    /// </summary>
    public async Task<string> ExportAsync(string? outputPath = null)
    {
        var installedPackages = winGetService.GetAllInstalledPackages();

        var packages = installedPackages.Select(p => new GistGetPackage
        {
            Id = p.Id.AsPrimitive(),
        }).ToList();

        var yaml = GistGetPackageSerializer.Serialize(packages);

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, yaml);
            consoleService.WriteInfo($"Exported {packages.Count} packages to {outputPath}");
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

        await gitHubService.SavePackagesAsync(
            credential.Token,
            "",
            Constants.DefaultGistFileName,
            Constants.DefaultGistDescription,
            packages);

        consoleService.WriteInfo($"Imported {packages.Count} packages to Gist");
    }
}
