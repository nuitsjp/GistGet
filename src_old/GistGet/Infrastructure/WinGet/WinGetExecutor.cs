using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GistGet.Infrastructure.OS;
using GistGet.Models;

namespace GistGet.Infrastructure.WinGet;

public class WinGetExecutor : IWinGetExecutor
{
    private readonly IProcessRunner _processRunner;
    private readonly string _wingetExe;

    public WinGetExecutor(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
        _wingetExe = ResolveWingetPath();
    }

    public async Task<bool> InstallPackageAsync(GistGetPackage package)
    {
        if (string.IsNullOrWhiteSpace(package.Id))
        {
            throw new ArgumentException("Package Id is required.", nameof(package));
        }

        var args = new List<string>
        {
            "install",
            "--id", package.Id,
            "--accept-source-agreements",
            "--accept-package-agreements"
        };

        // Interactivity handling
        if (package.Interactive)
        {
            args.Add("--interactive");
        }
        else if (package.Silent)
        {
            args.Add("--silent");
        }
        else
        {
            args.Add("--disable-interactivity");
        }

        if (!string.IsNullOrEmpty(package.Version))
        {
            args.Add("--version");
            args.Add(package.Version);
        }

        if (!string.IsNullOrEmpty(package.Scope))
        {
            args.Add("--scope");
            args.Add(package.Scope);
        }

        if (!string.IsNullOrEmpty(package.Architecture))
        {
            args.Add("--architecture");
            args.Add(package.Architecture);
        }

        if (!string.IsNullOrEmpty(package.Location))
        {
            args.Add("--location");
            args.Add(package.Location);
        }

        if (!string.IsNullOrEmpty(package.Log))
        {
            args.Add("--log");
            args.Add(package.Log);
        }

        if (!string.IsNullOrEmpty(package.Override))
        {
            args.Add("--override");
            args.Add(package.Override);
        }

        if (package.Force)
        {
            args.Add("--force");
        }

        if (package.SkipDependencies)
        {
            args.Add("--skip-dependencies");
        }

        if (!string.IsNullOrEmpty(package.Header))
        {
            args.Add("--header");
            args.Add(package.Header);
        }

        if (!string.IsNullOrEmpty(package.InstallerType))
        {
            args.Add("--installer-type");
            args.Add(package.InstallerType);
        }

        if (!string.IsNullOrEmpty(package.Custom))
        {
            args.AddRange(package.Custom.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        var exitCode = await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> UninstallPackageAsync(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package Id is required.", nameof(packageId));
        }

        var args = new List<string>
        {
            "uninstall",
            "--id", packageId,
            "--accept-source-agreements",
            "--disable-interactivity"
        };

        var exitCode = await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> UpgradePackageAsync(string packageId, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package Id is required.", nameof(packageId));
        }

        var args = new List<string> { "upgrade", "--id", packageId };
        if (!string.IsNullOrEmpty(version))
        {
            args.Add("--version");
            args.Add(version);
        }
        args.Add("--accept-source-agreements");
        args.Add("--accept-package-agreements");

        var exitCode = await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> PinPackageAsync(string packageId, string version)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package Id is required.", nameof(packageId));
        }

        var args = new List<string> { "pin", "add", "--id", packageId, "--version", version, "--force" };
        var exitCode = await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> UnpinPackageAsync(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package Id is required.", nameof(packageId));
        }

        var args = new List<string> { "pin", "remove", "--id", packageId };
        var exitCode = await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<int> RunPassthroughAsync(string command, string[] args)
    {
        var allArgs = new List<string> { command };
        allArgs.AddRange(args);
        return await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", allArgs));
    }

    private string ResolveWingetPath()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }
}
