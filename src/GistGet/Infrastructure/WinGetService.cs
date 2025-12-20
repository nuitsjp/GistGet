// WinGet COM-based package discovery implementation.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using GistGet.Infrastructure.WinGet;
using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure;

/// <summary>
/// Provides access to installed package data via WinGet COM APIs.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class WinGetService : IWinGetService
{
    private readonly IPackageCatalogConnector _connector;

    /// <summary>
    /// Regex pattern for parsing winget pin list data lines.
    /// Format: Name  Id  Version  Source  PinType  PinnedVersion
    /// Example: jq   jqlang.jq 1.7        winget Gating     1.7.0
    /// </summary>
    /// <remarks>
    /// Named groups: Name, Id (vendor.package format), Version, Source, PinType, PinnedVersion (optional)
    /// Uses ExplicitCapture, non-backtracking patterns, and timeout to avoid ReDoS.
    /// </remarks>
    [GeneratedRegex(@"^(?<Name>\S+)\s+(?<Id>\S+\.\S+)\s+(?<Version>\S+)\s+(?<Source>\S+)\s+(?<PinType>\S+)(?:\s+(?<PinnedVersion>\S+))?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PinLineRegex();

    /// <summary>
    /// Initializes a new instance with the default connector.
    /// </summary>
    public WinGetService() : this(new PackageCatalogConnector())
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom connector for testing.
    /// </summary>
    /// <param name="connector">Package catalog connector.</param>
    public WinGetService(IPackageCatalogConnector connector)
    {
        _connector = connector;
    }

    /// <summary>
    /// Finds an installed package by ID.
    /// </summary>
    /// <param name="id">Package ID.</param>
    /// <returns>The installed package, or <see langword="null"/> if not found.</returns>
    public WinGetPackage? FindById(PackageId id)
    {
        try
        {
            return FindByIdInternal(id);
        }
        catch (COMException)
        {
            return null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (TypeLoadException)
        {
            return null;
        }
    }

    private WinGetPackage? FindByIdInternal(PackageId id)
    {
        var package = FindByIdInCatalog(id, CompositeSearchBehavior.LocalCatalogs);
        if (package != null)
        {
            return package;
        }

        var installedPackages = GetAllInstalledPackages();
        package = installedPackages.FirstOrDefault(p =>
            string.Equals(p.Id.AsPrimitive(), id.AsPrimitive(), StringComparison.OrdinalIgnoreCase));
        if (package != null)
        {
            return package;
        }

        return FindByIdInCatalog(id, CompositeSearchBehavior.AllCatalogs);
    }

    private WinGetPackage? FindByIdInCatalog(PackageId id, CompositeSearchBehavior searchBehavior)
    {
        var catalog = _connector.Connect(searchBehavior);
        if (catalog == null)
        {
            return null;
        }

        foreach (var filter in CreatePackageFilters(id))
        {
            var findPackagesOptions = new FindPackagesOptions();
            findPackagesOptions.Selectors.Add(filter);

            var findResult = _connector.FindPackages(catalog, findPackagesOptions);
            var package = ExtractInstalledPackage(findResult);
            if (package != null)
            {
                return package;
            }
        }

        return null;
    }

    private static IEnumerable<PackageMatchFilter> CreatePackageFilters(PackageId id)
    {
        yield return new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        };

        yield return new PackageMatchFilter
        {
            Field = PackageMatchField.PackageFamilyName,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        };

        yield return new PackageMatchFilter
        {
            Field = PackageMatchField.ProductCode,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        };
    }

    private static WinGetPackage? ExtractInstalledPackage(FindPackagesResult findResult)
    {
        if (findResult.Status != FindPackagesResultStatus.Ok || findResult.Matches.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < findResult.Matches.Count; i++)
        {
            var catalogPackage = findResult.Matches[i].CatalogPackage;
            if (catalogPackage.InstalledVersion == null)
            {
                continue;
            }

            var installedVersion = catalogPackage.InstalledVersion;
            var usableVersion = GetUsableVersion(catalogPackage, installedVersion);
            var source = GetSourceName(catalogPackage);

            return new WinGetPackage(
                Name: catalogPackage.Name,
                Id: new PackageId(catalogPackage.Id),
                Version: new Version(installedVersion.Version),
                UsableVersion: usableVersion,
                Source: source
            );
        }

        return null;
    }

    /// <summary>
    /// Gets all locally installed packages.
    /// </summary>
    /// <returns>Installed packages.</returns>
    public IReadOnlyList<WinGetPackage> GetAllInstalledPackages()
    {
        try
        {
            return GetAllInstalledPackagesInternal();
        }
        catch (COMException)
        {
            return new List<WinGetPackage>();
        }
        catch (DllNotFoundException)
        {
            return new List<WinGetPackage>();
        }
        catch (TypeLoadException)
        {
            return new List<WinGetPackage>();
        }
    }

    private IReadOnlyList<WinGetPackage> GetAllInstalledPackagesInternal()
    {
        var packages = new List<WinGetPackage>();

        var catalog = _connector.Connect(CompositeSearchBehavior.LocalCatalogs);
        if (catalog == null)
        {
            return packages;
        }

        var findPackagesOptions = new FindPackagesOptions();
        var findResult = _connector.FindPackages(catalog, findPackagesOptions);

        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            return packages;
        }

        for (var i = 0; i < findResult.Matches.Count; i++)
        {
            var match = findResult.Matches[i];
            var catalogPackage = match.CatalogPackage;

            if (catalogPackage.InstalledVersion == null)
            {
                continue;
            }

            var installedVersion = catalogPackage.InstalledVersion;
            var usableVersion = GetUsableVersion(catalogPackage, installedVersion);

            var source = GetSourceName(catalogPackage);

            packages.Add(new WinGetPackage(
                Name: catalogPackage.Name,
                Id: new PackageId(catalogPackage.Id),
                Version: new Version(installedVersion.Version),
                UsableVersion: usableVersion,
                Source: source
            ));
        }

        return packages;
    }

    /// <summary>
    /// Determines usable version from available versions.
    /// </summary>
    private static Version? GetUsableVersion(CatalogPackage catalogPackage, PackageVersionInfo installedVersion)
    {
        // Check for available updates by comparing versions.
        // Note: IsUpdateAvailable performs applicability checks (architecture, requirements, pinning)
        // and may return false even when AvailableVersions contains newer versions (e.g., arm64-only on x64).
        // We use AvailableVersions[0] for simple version comparison without applicability constraints.
        if (catalogPackage.AvailableVersions.Count > 0)
        {
            var latestAvailableVersion = catalogPackage.AvailableVersions[0].Version;
            if (latestAvailableVersion != installedVersion.Version)
            {
                return new Version(latestAvailableVersion);
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the source name from the catalog package.
    /// </summary>
    private static string? GetSourceName(CatalogPackage catalogPackage)
    {
        for (var i = 0; i < catalogPackage.AvailableVersions.Count; i++)
        {
            var versionInfo = catalogPackage.GetPackageVersionInfo(catalogPackage.AvailableVersions[i]);
            if (versionInfo.Id == catalogPackage.Id)
            {
                return versionInfo.PackageCatalog.Info.Name;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all pinned packages by parsing winget pin list output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike FindById and GetAllInstalledPackages which use the WinGet COM API
    /// (Microsoft.Management.Deployment), this method uses CLI output parsing.
    /// </para>
    /// <para>
    /// This is because the WinGet COM API does not expose pin management functionality.
    /// Pin information is managed internally by WinGet in PinningIndex.cpp and PinningData.cpp,
    /// but these are not part of the public COM API surface (PackageManager.idl).
    /// </para>
    /// <para>
    /// The CLI approach is consistent with how GistGet handles other pin operations
    /// (pin add, pin remove) via WinGetPassthroughRunner.
    /// </para>
    /// </remarks>
    /// <returns>Pinned packages.</returns>
    [ExcludeFromCodeCoverage]
    public IReadOnlyList<WinGetPin> GetPinnedPackages()
    {
        var pins = new List<WinGetPin>();

        var output = RunWinGetPinList();
        if (string.IsNullOrEmpty(output))
        {
            return pins;
        }

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return pins;
        }

        // Find separator line (contains dashes)
        var separatorIndex = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("---"))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 0)
        {
            return pins;
        }

        // Parse data lines using whitespace splitting (language-independent)
        for (var i = separatorIndex + 1; i < lines.Length; i++)
        {
            var pin = ParsePinLineByWhitespace(lines[i]);
            if (pin != null)
            {
                pins.Add(pin);
            }
        }

        return pins;
    }

    [ExcludeFromCodeCoverage]
    private static string RunWinGetPinList()
    {
        try
        {
            var wingetPath = ResolveWinGetPath();

            var startInfo = new ProcessStartInfo
            {
                FileName = wingetPath,
                Arguments = "pin list",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        catch
        {
            // Process execution can fail in certain environments (e.g., restricted contexts)
            return string.Empty;
        }
    }

    [ExcludeFromCodeCoverage]
    private static string ResolveWinGetPath()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, "winget.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }

    /// <summary>
    /// Parses a pin data line using regex pattern matching.
    /// This approach is language-independent and anchors on the package ID format.
    /// </summary>
    /// <remarks>
    /// Expected format: Name  Id  Version  Source  PinType  PinnedVersion
    /// Example: jq   jqlang.jq 1.7        winget Gating     1.7.0
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private static WinGetPin? ParsePinLineByWhitespace(string line)
    {
        try
        {
            var match = PinLineRegex().Match(line.Trim());
            if (!match.Success)
            {
                return null;
            }

            // Named groups: Name, Id, Version, Source, PinType, PinnedVersion
            var id = match.Groups["Id"].Value.Trim();
            var pinType = match.Groups["PinType"].Value.Trim();
            var pinnedVersion = match.Groups["PinnedVersion"].Success ? match.Groups["PinnedVersion"].Value.Trim() : string.Empty;

            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return new WinGetPin(
                Id: new PackageId(id),
                PinType: pinType,
                PinnedVersion: string.IsNullOrEmpty(pinnedVersion) ? null : new Version(pinnedVersion)
            );
        }
        catch
        {
            return null;
        }
    }
}

