// Builds WinGet command-line arguments from options and package entries.

namespace GistGet.Infrastructure;

/// <summary>
/// Constructs argument arrays for invoking WinGet.
/// </summary>
public class WinGetArgumentBuilder : IWinGetArgumentBuilder
{
    /// <summary>
    /// Builds arguments for an install command.
    /// </summary>
    public string[] BuildInstallArgs(InstallOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var args = new List<string> { "install", "--id", options.Id };

        if (!string.IsNullOrEmpty(options.Version)) { args.Add("--version"); args.Add(options.Version); }

        args.AddRange(BuildCommonInstallOptions(
            options.Scope, options.Architecture, options.Location,
            options.Interactive, options.Silent, options.Log,
            options.Override, options.Force, options.SkipDependencies,
            options.Header, options.InstallerType, options.Custom,
            options.Locale, options.AcceptPackageAgreements,
            options.AcceptSourceAgreements, options.AllowHashMismatch));

        return args.ToArray();
    }

    public string[] BuildInstallArgs(GistGetPackage package)
    {
        var args = new List<string> { "install", "--id", package.Id };

        if (!string.IsNullOrEmpty(package.Pin)) { args.Add("--version"); args.Add(package.Pin); }
        else if (!string.IsNullOrEmpty(package.Version)) { args.Add("--version"); args.Add(package.Version); }

        args.AddRange(BuildCommonInstallOptions(
            package.Scope, package.Architecture, package.Location,
            package.Interactive, package.Silent, package.Log,
            package.Override, package.Force, package.SkipDependencies,
            package.Header, package.InstallerType, package.Custom,
            package.Locale, package.AcceptPackageAgreements,
            package.AcceptSourceAgreements, package.AllowHashMismatch));

        return args.ToArray();
    }

    public string[] BuildUpgradeArgs(UpgradeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var args = new List<string> { "upgrade", "--id", options.Id };

        if (!string.IsNullOrEmpty(options.Version)) { args.Add("--version"); args.Add(options.Version); }
        args.AddRange(BuildCommonInstallOptions(
            options.Scope, options.Architecture, options.Location,
            options.Interactive, options.Silent, options.Log,
            options.Override, options.Force, options.SkipDependencies,
            options.Header, options.InstallerType, options.Custom,
            options.Locale, options.AcceptPackageAgreements,
            options.AcceptSourceAgreements, options.AllowHashMismatch));

        return args.ToArray();
    }

    public string[] BuildUninstallArgs(UninstallOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var args = new List<string> { "uninstall", "--id", options.Id };

        if (options.Silent) args.Add("--silent");
        if (options.Interactive) args.Add("--interactive");
        if (options.Force) args.Add("--force");
        if (!string.IsNullOrEmpty(options.Scope)) { args.Add("--scope"); args.Add(options.Scope); }

        return args.ToArray();
    }

    public string[] BuildPinAddArgs(string id, string version, string? pinType = null, bool force = false)
    {
        var args = new List<string> { "pin", "add", "--id", id, "--version", version };

        if (force) args.Add("--force");

        if (!string.IsNullOrEmpty(pinType))
        {
            if (pinType.Equals("blocking", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("--blocking");
            }
            else if (pinType.Equals("gating", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("--gating");
            }
        }

        return args.ToArray();
    }

    private static List<string> BuildCommonInstallOptions(
        string? scope, string? architecture, string? location,
        bool interactive, bool silent, string? log,
        string? overrideArgs, bool force, bool skipDependencies,
        string? header, string? installerType, string? custom,
        string? locale, bool acceptPackageAgreements,
        bool acceptSourceAgreements, bool allowHashMismatch)
    {
        var args = new List<string>();

        if (silent) args.Add("--silent");
        if (interactive) args.Add("--interactive");
        if (force) args.Add("--force");
        if (acceptPackageAgreements) args.Add("--accept-package-agreements");
        if (acceptSourceAgreements) args.Add("--accept-source-agreements");
        if (allowHashMismatch) args.Add("--ignore-security-hash");
        if (skipDependencies) args.Add("--skip-dependencies");

        if (!string.IsNullOrEmpty(scope)) { args.Add("--scope"); args.Add(scope); }
        if (!string.IsNullOrEmpty(architecture)) { args.Add("--architecture"); args.Add(architecture); }
        if (!string.IsNullOrEmpty(location)) { args.Add("--location"); args.Add(location); }
        if (!string.IsNullOrEmpty(log)) { args.Add("--log"); args.Add(log); }
        if (!string.IsNullOrEmpty(header)) { args.Add("--header"); args.Add(header); }
        if (!string.IsNullOrEmpty(custom)) { args.Add("--custom"); args.Add(custom); }
        if (!string.IsNullOrEmpty(overrideArgs)) { args.Add("--override"); args.Add(overrideArgs); }
        if (!string.IsNullOrEmpty(installerType)) { args.Add("--installer-type"); args.Add(installerType); }
        if (!string.IsNullOrEmpty(locale)) { args.Add("--locale"); args.Add(locale); }

        return args;
    }
}

