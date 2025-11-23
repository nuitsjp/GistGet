using GistGet.Infrastructure.OS;
using GistGet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
        var args = new List<string>
        {
            "install",
            "--id", package.Id,
            "--accept-source-agreements",
            "--accept-package-agreements",
            "--disable-interactivity"
        };

        if (!string.IsNullOrEmpty(package.Version))
        {
            args.Add("--version");
            args.Add(package.Version);
        }

        if (!string.IsNullOrEmpty(package.Custom))
        {
            args.AddRange(package.Custom.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        var (exitCode, _, _) = await _processRunner.RunAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> UninstallPackageAsync(string packageId)
    {
        var args = new List<string>
        {
            "uninstall",
            "--id", packageId,
            "--accept-source-agreements",
            "--disable-interactivity"
        };

        var (exitCode, _, _) = await _processRunner.RunAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> UpgradePackageAsync(string packageId, string? version = null)
    {
        var args = new List<string> { "upgrade", "--id", packageId };
        if (!string.IsNullOrEmpty(version))
        {
            args.Add("--version");
            args.Add(version);
        }
        args.Add("--accept-source-agreements");
        args.Add("--accept-package-agreements");

        var (exitCode, _, _) = await _processRunner.RunAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> PinPackageAsync(string packageId, string version)
    {
        var args = new List<string> { "pin", "add", "--id", packageId, "--version", version, "--force" };
        var (exitCode, _, _) = await _processRunner.RunAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task<bool> UnpinPackageAsync(string packageId)
    {
        var args = new List<string> { "pin", "remove", "--id", packageId };
        var (exitCode, _, _) = await _processRunner.RunAsync(_wingetExe, string.Join(" ", args));
        return exitCode == 0;
    }

    public async Task RunPassthroughAsync(string command, string[] args)
    {
        var allArgs = new List<string> { command };
        allArgs.AddRange(args);
        await _processRunner.RunPassthroughAsync(_wingetExe, string.Join(" ", allArgs));
    }

    private string ResolveWingetPath()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }
}
