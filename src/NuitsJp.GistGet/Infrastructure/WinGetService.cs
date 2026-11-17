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
        var sources = ReadPackageSources();
        var canonicalId = sources.Keys.FirstOrDefault(packageId =>
            string.Equals(packageId, id.AsPrimitive(), StringComparison.OrdinalIgnoreCase)) ?? id.AsPrimitive();
        return ReadPackages(["--id", canonicalId, "--exact"], sources: sources)
            .FirstOrDefault(p => string.Equals(p.Id.AsPrimitive(), id.AsPrimitive(), StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<WinGetPackage> GetAllInstalledPackages()
    {
        return ReadPackages([]);
    }

    public IReadOnlyList<WinGetPackage> GetPackagesWithUpdates()
    {
        return GetPackagesWithUpdates(includeUnknown: false, includePinned: false);
    }

    public IReadOnlyList<WinGetPackage> GetPackagesWithUpdates(bool includeUnknown, bool includePinned)
    {
        var options = new List<string> { "--upgrade-available" };
        if (includePinned)
        {
            options.Add("--include-pinned");
        }

        if (includeUnknown)
        {
            options.Add("--include-unknown");
        }

        return ReadPackages(options, updatesOnly: true)
            .Where(p => p.UsableVersion != null && (includeUnknown || !p.IsVersionUnknown))
            .ToList();
    }

    private IReadOnlyList<WinGetPackage> ReadPackages(IEnumerable<string> options, bool updatesOnly = false,
        Dictionary<string, string?>? sources = null)
    {
        var arguments = new List<string> { "list", "--details", "--sort", "version", "--descending", "--accept-source-agreements", "--disable-interactivity" };
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

        sources ??= ReadPackageSources();

        return packages.Select(p => p with
        {
            Source = p.Source ?? sources.GetValueOrDefault(p.Id.AsPrimitive())
        }).ToList();
    }

    private Dictionary<string, string?> ReadPackageSources()
    {
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

        return sources;
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
                    if (IsNoInstalledPackageMessage(line))
                    {
                        return [];
                    }
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
        if ((total.HasValue && packages.Count != total.Value) || packages.Count == 0)
        {
            throw new InvalidOperationException("WinGet returned an empty or incomplete package list.");
        }

        // WinGet sorts using its own version semantics, including '<' and '>' versions.
        return packages.GroupBy(p => p.Id.AsPrimitive(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var newest = group.First();
                return group.FirstOrDefault(p => p.Version == newest.Version && p.UsableVersion != null) ?? newest;
            })
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

    // WinGet v1.29.380 NoInstalledPackageFound resource, for the supported UI languages.
    private static bool IsNoInstalledPackageMessage(string line)
    {
        return line.Trim() is
            "No installed package found matching input criteria." or
            "Es wurde kein installiertes Paket gefunden, das den Eingabekriterien entspricht." or
            "No se encontró ningún paquete que coincida con los criterios de entrada." or
            "Aucun package installé ne correspond aux critères saisis." or
            "Non è stato trovato alcun pacchetto installato corrispondente ai criteri di input." or
            "入力条件に一致するインストール済みのパッケージが見つかりませんでした。" or
            "입력 조건과 일치하는 설치된 패키지를 찾을 수 없습니다." or
            "Nenhum pacote instalado foi encontrado que corresponda aos critérios de entrada." or
            "Не найдены установленные пакеты, соответствующие введенным условиям." or
            "找不到与输入条件匹配的已安装程序包。" or
            "未找到符合輸入條件的已安裝套件。";
    }

    private static bool ContainsSourceFailure(string output)
    {
        // SourceOpenWithFailedUpdate and SearchFailureWarning are written to stdout,
        // even when WinGet returns success or NoApplicationsFound.
        string[] prefixes =
        [
            "Failed in attempting to update the source: ",
            "Failed when searching source; results will not be included: ",
            "Fehler beim Versuch, die Quelle zu aktualisieren: ",
            "Fehler beim Durchsuchen der Quelle. Ergebnisse werden nicht einbezogen: ",
            "Error al intentar actualizar el origen: ",
            "Error al buscar en el origen, los resultados no se incluirán: ",
            "Échec de la tentative de mise à jour de la source : ",
            "Échec lors de la recherche de la source\u00a0; les résultats ne seront pas inclus\u00a0: ",
            "Non è stato possibile aggiornare l'origine: ",
            "Errore durante la ricerca nell'origine; i risultati non verranno inclusi: '",
            "ソースを更新できませんでした: ",
            "ソースの検索中にエラーが発生しました;結果は含まれません: ",
            "원본을 업데이트하지 못했습니다. ",
            "원본을 검색하는 동안 실패했습니다. 결과가 포함되지 않습니다. ",
            "Falha ao tentar atualizar a origem: ",
            "Falha ao pesquisar origem; os resultados não serão incluídos: ",
            "Не удалось обновить источник: ",
            "Сбой при поиске в источнике; результаты не будут включены: ",
            "尝试更新源失败： ",
            "搜索源时失败;结果将不包括在内： ",
            "嘗試更新來源失敗： ",
            "搜尋來源時失敗; 將不會包含結果: "
        ];
        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Any(line => prefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)));
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
        if (ContainsSourceFailure(result.StandardOutput) ||
            (result.ExitCode != 0 && !(allowNoApplications && result.ExitCode == NoApplicationsFound)))
        {
            throw new InvalidOperationException(
                $"winget {string.Join(' ', startInfo.ArgumentList)} failed (0x{result.ExitCode:X8}). {result.StandardError} {result.StandardOutput}");
        }

        return result;
    }
}

