// WinGet COM-based package discovery implementation.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GistGet.Infrastructure.WinGet;
using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure;

/// <summary>
/// Provides access to installed package data via WinGet COM APIs.
/// </summary>
public class WinGetService : IWinGetService
{
    private readonly IPackageCatalogConnector _connector;

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
        var catalog = _connector.Connect(CompositeSearchBehavior.AllCatalogs);
        if (catalog == null)
        {
            return null;
        }

        var findPackagesOptions = new FindPackagesOptions();
        findPackagesOptions.Selectors.Add(new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        });

        var findResult = _connector.FindPackages(catalog, findPackagesOptions);

        if (findResult.Status != FindPackagesResultStatus.Ok || findResult.Matches.Count == 0)
        {
            return null;
        }

        var catalogPackage = findResult.Matches[0].CatalogPackage;

        if (catalogPackage.InstalledVersion == null)
        {
            return null;
        }

        var installedVersion = catalogPackage.InstalledVersion;
        var usableVersion = GetUsableVersion(catalogPackage, installedVersion);

        return new WinGetPackage(
            Name: catalogPackage.Name,
            Id: new PackageId(catalogPackage.Id),
            Version: new Version(installedVersion.Version),
            UsableVersion: usableVersion
        );
    }

    /// <summary>
    /// Gets all locally installed packages.
    /// </summary>
    /// <returns>Installed packages.</returns>
    public IReadOnlyList<WinGetPackage> GetAllInstalledPackages()
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

            packages.Add(new WinGetPackage(
                Name: catalogPackage.Name,
                Id: new PackageId(catalogPackage.Id),
                Version: new Version(installedVersion.Version),
                UsableVersion: usableVersion
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

        // Find header line (contains dashes separator)
        var headerIndex = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("---"))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 1)
        {
            return pins;
        }

        // Parse column positions from header
        var headerLine = lines[headerIndex - 1];
        var columnPositions = ParseColumnPositions(headerLine);

        // Parse data lines
        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            var pin = ParsePinLine(lines[i], columnPositions);
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

    [ExcludeFromCodeCoverage]
    private static int[] ParseColumnPositions(string headerLine)
    {
        // Find positions where each column starts based on whitespace patterns
        var positions = new List<int> { 0 };
        var inWhitespace = false;

        for (var i = 1; i < headerLine.Length; i++)
        {
            if (char.IsWhiteSpace(headerLine[i]))
            {
                inWhitespace = true;
            }
            else if (inWhitespace)
            {
                positions.Add(i);
                inWhitespace = false;
            }
        }

        return [.. positions];
    }

    [ExcludeFromCodeCoverage]
    private static WinGetPin? ParsePinLine(string line, int[] columnPositions)
    {
        if (columnPositions.Length < 5)
        {
            return null;
        }

        try
        {
            // Column order: Name, Id, Version, Source, PinType, PinnedVersion
            var id = ExtractColumn(line, columnPositions, 1).Trim();
            var pinType = ExtractColumn(line, columnPositions, 4).Trim();
            var pinnedVersion = ExtractColumn(line, columnPositions, 5).Trim();

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

    [ExcludeFromCodeCoverage]
    private static string ExtractColumn(string line, int[] positions, int columnIndex)
    {
        if (columnIndex >= positions.Length)
        {
            return columnIndex == positions.Length && positions.Length > 0
                ? line[positions[^1]..].Trim()
                : string.Empty;
        }

        var start = positions[columnIndex];
        var end = columnIndex + 1 < positions.Length ? positions[columnIndex + 1] : line.Length;

        if (start >= line.Length)
        {
            return string.Empty;
        }

        end = Math.Min(end, line.Length);
        return line[start..end];
    }
}

