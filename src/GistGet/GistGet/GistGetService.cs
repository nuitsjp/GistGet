using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace GistGet;

public class GistGetService(
    IGitHubService gitHubService,
    IConsoleService consoleService,
    ICredentialService credentialService,
    IWinGetPassthroughRunner passthroughRunner) 
    : IGistGetService
{
    public async Task AuthLoginAsync()
    {
        var credential = await gitHubService.LoginAsync();
        credentialService.SaveCredential("git:https://github.com", credential);
    }

    public void AuthLogout()
    {
        credentialService.DeleteCredential("git:https://github.com");
        consoleService.WriteInfo("Logged out");
    }

    public void AuthStatus()
    {
        if (credentialService.TryGetCredential("git:https://github.com", out var credential))
        {
             var maskedToken = !string.IsNullOrEmpty(credential.Token) ? new string('*', credential.Token.Length) : "**********";

             consoleService.WriteInfo("github.com");
             consoleService.WriteInfo($"  ✓ Logged in to github.com as {credential.Username} (keyring)");
             consoleService.WriteInfo($"  ✓ Token: {maskedToken}");
        }
        else
        {
            consoleService.WriteInfo("You are not logged in.");
        }
    }

    public async Task InstallAndSaveAsync(GistGetPackage package)
    {
        // 1. Auth Check & Login
        if (!credentialService.TryGetCredential("git:https://github.com", out var credential) || credential == null)
        {
            await AuthLoginAsync();
            if (!credentialService.TryGetCredential("git:https://github.com", out credential) || credential == null)
            {
                throw new InvalidOperationException("Failed to retrieve credentials after login.");
            }
        }

        // 2. Fetch Gist Packages
        var existingPackages = await gitHubService.GetPackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages");
        var existingPackage = existingPackages.FirstOrDefault(p => string.Equals(p.Id, package.Id, StringComparison.OrdinalIgnoreCase));

        // 3. Pin Logic & Install Version Resolution
        string? installVersion = package.Version;
        string? pinVersionToSet = null;
        string? pinTypeToSet = existingPackage?.PinType; // Default to existing pin type if not specified in install args (which GistGetPackage doesn't explicitly support in args yet, assuming Gist state prevails)

        if (!string.IsNullOrEmpty(package.Version))
        {
            // CLI explicitly specified version
            installVersion = package.Version;

            if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
            {
                // Gist has Pin, update it to matched installed version
                pinVersionToSet = package.Version;
            }
        }
        else
        {
            // CLI did not specify version
            if (existingPackage != null && !string.IsNullOrEmpty(existingPackage.Pin))
            {
                // Gist has Pin, use it
                installVersion = existingPackage.Pin;
                pinVersionToSet = existingPackage.Pin;
            }
        }

        // 4. Construct WinGet Install Command
        var installArgs = new List<string> { "install", "--id", package.Id };
        if (!string.IsNullOrEmpty(installVersion))
        {
            installArgs.Add("--version");
            installArgs.Add(installVersion);
        }

        // Add other flags from package object
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
        if (package.Custom != null) installArgs.Add(package.Custom); // Custom args usually parsed string, simplified here
        if (package.Override != null) { installArgs.Add("--override"); installArgs.Add(package.Override); }


        // 5. Run WinGet Install
        var exitCode = await passthroughRunner.RunAsync(installArgs.ToArray());
        if (exitCode != 0)
        {
            // Install failed, do not save to Gist
            return;
        }

        // 6. Run WinGet Pin Add (if needed)
        if (!string.IsNullOrEmpty(pinVersionToSet))
        {
            var pinArgs = new List<string> { "pin", "add", "--id", package.Id, "--version", pinVersionToSet };
            if (!string.IsNullOrEmpty(pinTypeToSet))
            {
                if (pinTypeToSet.Equals("blocking", StringComparison.OrdinalIgnoreCase)) pinArgs.Add("--blocking");
                // gating is complex with wildcards, simplistic handling here if needed
            }
             await passthroughRunner.RunAsync(pinArgs.ToArray());
        }


        // 7. Update Gist Package List & Save
        var newPackagesList = existingPackages.Where(p => !string.Equals(p.Id, package.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        
        // Create new package entry merging CLI args and Gist state
        var packageToSave = new GistGetPackage
        {
            Id = package.Id,
            Version = installVersion, // Save the installed version? Or just keep it generic? Spec says "install ... saves to Gist". Usually we save the version if pinned or explicit?
                                      // If we installed a specific version, we might record it. But if it's "latest", we usually don't verify what latest is here.
                                      // However, for Pin logic:
            Pin = pinVersionToSet,
            PinType = pinTypeToSet,
            
            // Merge other properties (CLI overrides Gist if specified, otherwise keep Gist, or defaults?)
            // Simple approach: Use CLI package properties, but preserve Pin/PinType if not handled above.
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
            
            // Preserve uninstall flag? If we just installed it, uninstall should be false/null.
            Uninstall = false 
        };
        
        // If we didn't specify version in CLI/Pin, we might enter it as null in Gist? 
        // Spec: "install ... --version ... updates Gist".
        if (string.IsNullOrEmpty(packageToSave.Version) && !string.IsNullOrEmpty(installVersion))
        {
             packageToSave.Version = installVersion;
        }

        newPackagesList.Add(packageToSave);
        
        await gitHubService.SavePackagesAsync(credential.Token, "", "packages.yaml", "GistGet packages", newPackagesList);
    }

    public Task UninstallAndSaveAsync(string packageId)
    {
        throw new NotImplementedException();
    }

    public Task UpgradeAndSaveAsync(string packageId, string? version = null)
    {
        throw new NotImplementedException();
    }

    public Task PinAddAndSaveAsync(string packageId, string version)
    {
        throw new NotImplementedException();
    }

    public Task PinRemoveAndSaveAsync(string packageId)
    {
        throw new NotImplementedException();
    }

    public Task<int> RunPassthroughAsync(string command, string[] args)
    {
        var fullArgs = new List<string> { command };
        fullArgs.AddRange(args);
        return passthroughRunner.RunAsync(fullArgs.ToArray());
    }
}