// Package discovery through the installed Windows Package Manager CLI.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NuitsJp.GistGet.Infrastructure.Diagnostics;
using Spectre.Console.Rendering;

namespace NuitsJp.GistGet.Infrastructure;

/// <summary>
/// Reads package information from the installed winget.exe.
/// </summary>
public partial class WinGetService(IProcessRunner processRunner) : IWinGetService
{
    private const int NoApplicationsFound = unchecked((int)0x8A150014);

    public WinGetService() : this(new ProcessRunner())
    {
    }

    [GeneratedRegex(@"^(?:\((?<Index>\d+)/(?<Total>\d+)\) )?(?<Name>.+) \[(?<Id>[^\]]+)\]$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PackageHeaderRegex();

    [GeneratedRegex(@"^  (?<Source>.+) \[(?<Version>[^\]]+)\]$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UpgradeRegex();

    [GeneratedRegex(@"(?<!\S)(?:Id|ID|장치 ID|ИД|識別碼)(?!\S)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdColumnRegex();

    [GeneratedRegex(@"\s+(?<Source>\S+)\s+(?<Type>Gating|Blocking|Pinning)(?:\s+(?<Version>\S+))?\s*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PinSuffixRegex();

    public WinGetPackage? FindById(PackageId id)
    {
        return ReadPackages(["--id", id.AsPrimitive(), "--exact"])
            .FirstOrDefault(p => string.Equals(p.Id.AsPrimitive(), id.AsPrimitive(), StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<WinGetPackage> GetAllInstalledPackages()
    {
        return ReadPackages([]);
    }

    public IReadOnlyList<WinGetPackage> GetPackagesWithUpdates()
    {
        return GetPackagesWithUpdates(includeUnknown: false);
    }

    public IReadOnlyList<WinGetPackage> GetPackagesWithUpdates(bool includeUnknown)
    {
        var options = new List<string> { "--upgrade-available" };
        if (includeUnknown)
        {
            options.Add("--include-unknown");
        }

        return ReadPackages(options, updatesOnly: true)
            .Where(p => p.UsableVersion != null && (includeUnknown || !p.IsVersionUnknown))
            .ToList();
    }

    private IReadOnlyList<WinGetPackage> ReadPackages(IEnumerable<string> options, bool updatesOnly = false)
    {
        var arguments = new List<string> { "list", "--details", "--accept-source-agreements", "--disable-interactivity" };
        arguments.AddRange(options);
        var result = Run(arguments, allowNoApplications: true);
        if (result.ExitCode == NoApplicationsFound)
        {
            return [];
        }

        var packages = ParsePackages(result.StandardOutput, updatesOnly);
        if (packages.Count == 0)
        {
            return packages;
        }

        var sourceResult = Run(["dscv3", "package", "--export", "--disable-interactivity"]);
        var sources = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var line in sourceResult.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                using var document = JsonDocument.Parse(line);
                var item = document.RootElement;
                var packageId = item.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException("WinGet returned a package without an ID.");
                sources[packageId] = item.TryGetProperty("source", out var source) ? source.GetString() : null;
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("WinGet returned invalid package source data.", exception);
        }

        return packages.Select(p => p with
        {
            Source = p.Source ?? sources.GetValueOrDefault(p.Id.AsPrimitive())
        }).ToList();
    }

    private static IReadOnlyList<WinGetPackage> ParsePackages(string output, bool updatesOnly)
    {
        var packages = new List<WinGetPackage>();
        string? name = null;
        string? id = null;
        Version? installedVersion = null;
        Version? usableVersion = null;
        string? source = null;
        int? total = null;

        void CompletePackage()
        {
            if (id == null)
            {
                return;
            }

            if (installedVersion == null)
            {
                throw new InvalidOperationException($"WinGet did not return an installed version for '{id}'.");
            }

            packages.Add(new WinGetPackage(name!, new PackageId(id), installedVersion.Value, usableVersion, source));
            id = null;
        }

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!char.IsWhiteSpace(line[0]))
            {
                var header = PackageHeaderRegex().Match(line);
                if (header.Success)
                {
                    CompletePackage();
                    if (total.HasValue && packages.Count == total.Value)
                    {
                        break;
                    }

                    if (header.Groups["Total"].Success)
                    {
                        var count = int.Parse(header.Groups["Total"].Value, CultureInfo.InvariantCulture);
                        var index = int.Parse(header.Groups["Index"].Value, CultureInfo.InvariantCulture);
                        if ((total.HasValue && total.Value != count) || index != packages.Count + 1)
                        {
                            throw new InvalidOperationException("WinGet returned an incomplete package list.");
                        }

                        total = count;
                    }

                    name = header.Groups["Name"].Value;
                    id = header.Groups["Id"].Value;
                    installedVersion = null;
                    usableVersion = null;
                    source = null;
                    continue;
                }
            }

            if (id == null)
            {
                if (updatesOnly)
                {
                    // A successful upgrade query can contain only the no-updates summary.
                    break;
                }

                continue;
            }

            if (installedVersion == null)
            {
                // Version is the first detail field; its localized label is not parsed.
                var separator = line.IndexOf(':', StringComparison.Ordinal);
                if (separator < 0 || string.IsNullOrWhiteSpace(line[(separator + 1)..]))
                {
                    throw new InvalidOperationException($"WinGet returned an invalid version for '{id}'.");
                }

                installedVersion = new Version(line[(separator + 1)..].Trim());
                continue;
            }

            var upgrade = UpgradeRegex().Match(line);
            if (upgrade.Success && usableVersion == null)
            {
                usableVersion = new Version(upgrade.Groups["Version"].Value);
                source = upgrade.Groups["Source"].Value;
            }
            else if (!char.IsWhiteSpace(line[0]) && !line.Contains(':', StringComparison.Ordinal))
            {
                // Summaries delimit packages that require explicit targeting or are pinned.
                CompletePackage();
                break;
            }
        }

        CompletePackage();
        if ((total.HasValue && packages.Count != total.Value) || (!updatesOnly && packages.Count == 0))
        {
            throw new InvalidOperationException("WinGet returned an empty or incomplete package list.");
        }

        var versionComparer = CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(CompareOptions.NumericOrdering);
        return packages.GroupBy(p => p.Id.AsPrimitive(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(p => p.IsVersionUnknown)
                .ThenByDescending(p => p.Version.AsPrimitive(), versionComparer)
                .ThenByDescending(p => p.UsableVersion != null)
                .First())
            .ToList();
    }

    public IReadOnlyList<WinGetPin> GetPinnedPackages()
    {
        var result = Run(["pin", "list", "--accept-source-agreements", "--disable-interactivity"]);
        var lines = result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var separator = Array.FindIndex(lines, line => line.Length > 0 && line.All(c => c == '-'));
        if (separator < 0)
        {
            // WinGet reports an empty pinning index as a successful message without a table.
            return [];
        }

        if (separator == 0)
        {
            throw new InvalidOperationException("WinGet returned a pin table without a header.");
        }

        var header = lines[separator - 1];
        var idColumn = IdColumnRegex().Match(header);
        if (!idColumn.Success)
        {
            throw new InvalidOperationException("WinGet returned an unrecognized pin table.");
        }

        var idOffset = new Segment(header[..idColumn.Index]).CellCount();
        var pins = new List<WinGetPin>();
        foreach (var line in lines.Skip(separator + 1))
        {
            var suffix = PinSuffixRegex().Match(line);
            var idStart = GetCharacterIndex(line, idOffset);
            if (!suffix.Success || suffix.Index <= idStart)
            {
                throw new InvalidOperationException($"WinGet returned an invalid pin: {line}");
            }

            var columns = line[idStart..suffix.Index].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length == 0 || columns[0].Contains('…', StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"WinGet returned an incomplete pin: {line}");
            }

            var pinnedVersion = suffix.Groups["Version"];
            pins.Add(new WinGetPin(new PackageId(columns[0]), suffix.Groups["Type"].Value,
                pinnedVersion.Success ? new Version(pinnedVersion.Value) : null));
        }

        return pins;
    }

    private static int GetCharacterIndex(string text, int cells)
    {
        var index = 0;
        var width = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (width >= cells)
            {
                return index;
            }

            width += new Segment(rune.ToString()).CellCount();
            index += rune.Utf16SequenceLength;
        }

        return index;
    }

    private ProcessResult Run(IEnumerable<string> arguments, bool allowNoApplications = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var result = processRunner.RunAndCapture(startInfo);
        if (result.ExitCode != 0 && !(allowNoApplications && result.ExitCode == NoApplicationsFound))
        {
            throw new InvalidOperationException(
                $"winget {string.Join(' ', startInfo.ArgumentList)} failed (0x{result.ExitCode:X8}). {result.StandardError} {result.StandardOutput}");
        }

        return result;
    }
}

