using System.Text.Json;
using System.Text.RegularExpressions;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Infrastructure.WinGet;

public class WinGetPassthroughTests
{
    private readonly MockWinGetPassthroughClient _mockPassthroughClient;

    public WinGetPassthroughTests()
    {
        _mockPassthroughClient = new MockWinGetPassthroughClient();
    }

    [Fact]
    public async Task ExecuteAsync_ListCommand_ShouldProduceNormalizedSnapshot()
    {
        // Arrange
        var args = new[] { "list" };

        // Act
        var result = await _mockPassthroughClient.ExecuteAsync(args);
        var normalizedOutput = NormalizeOutput(_mockPassthroughClient.LastOutput!);

        // Assert
        result.ShouldBe(0);
        normalizedOutput.ShouldBe(
            "Name|Id|Version|Source\nGit for Windows|Git.Git|2.43.0|winget\nNotePad++|Notepad++.Notepad++|8.6.8|winget");
    }

    [Fact]
    public async Task ExecuteAsync_ExportCommand_ShouldProduceNormalizedJsonSnapshot()
    {
        // Arrange
        var args = new[] { "export", "--output", "packages.json" };

        // Act
        var result = await _mockPassthroughClient.ExecuteAsync(args);
        var normalizedOutput = NormalizeJsonOutput(_mockPassthroughClient.LastOutput!);

        // Assert
        result.ShouldBe(0);
        var expectedJson = """
                           {
                             "Sources": [
                               {
                                 "Packages": [
                                   {
                                     "PackageIdentifier": "Git.Git",
                                     "Version": "2.43.0"
                                   }
                                 ],
                                 "SourceDetails": {
                                   "Argument": "https://cdn.winget.microsoft.com/cache",
                                   "Identifier": "winget",
                                   "Name": "winget",
                                   "Type": "Microsoft.PreIndexed.Package"
                                 }
                               }
                             ],
                             "WinGetVersion": "1.7.10661"
                           }
                           """;
        normalizedOutput.ShouldBe(expectedJson);
    }

    [Fact]
    public async Task ExecuteAsync_SearchCommand_ShouldProduceNormalizedSnapshot()
    {
        // Arrange
        var args = new[] { "search", "notepad" };

        // Act
        var result = await _mockPassthroughClient.ExecuteAsync(args);
        var normalizedOutput = NormalizeOutput(_mockPassthroughClient.LastOutput!);

        // Assert
        result.ShouldBe(0);
        normalizedOutput.ShouldBe("Name|Id|Version|Source\nNotePad++|Notepad++.Notepad++|8.6.8|winget");
    }

    [Fact]
    public async Task ExecuteAsync_ShowCommand_ShouldProduceNormalizedSnapshot()
    {
        // Arrange
        var args = new[] { "show", "Git.Git" };

        // Act
        var result = await _mockPassthroughClient.ExecuteAsync(args);
        var normalizedOutput = NormalizeOutput(_mockPassthroughClient.LastOutput!);

        // Assert
        result.ShouldBe(0);
        normalizedOutput.ShouldBe(
            "Found Git for Windows [Git.Git]\nVersion: 2.43.0\nPublisher: The Git Development Community");
    }

    [Fact]
    public async Task ExecuteAsync_ImportCommand_ShouldProduceNormalizedSnapshot()
    {
        // Arrange
        var args = new[] { "import", "--input", "packages.json", "--accept-package-agreements" };

        // Act
        var result = await _mockPassthroughClient.ExecuteAsync(args);
        var normalizedOutput = NormalizeOutput(_mockPassthroughClient.LastOutput!);

        // Assert
        result.ShouldBe(0);
        normalizedOutput.ShouldBe("Successfully imported packages.");
    }

    [Theory]
    [InlineData(new[] { "list" },
        "Name|Id|Version|Source\nGit for Windows|Git.Git|2.43.0|winget\nNotePad++|Notepad++.Notepad++|8.6.8|winget")]
    [InlineData(new[] { "search", "notepad" }, "Name|Id|Version|Source\nNotePad++|Notepad++.Notepad++|8.6.8|winget")]
    [InlineData(new[] { "show", "Git.Git" },
        "Found Git for Windows [Git.Git]\nVersion: 2.43.0\nPublisher: The Git Development Community")]
    [InlineData(new[] { "import", "--input", "test.json" }, "Successfully imported packages.")]
    public async Task ExecuteAsync_RoundTripNormalization_ShouldProduceDeterministicOutput(string[] args,
        string expectedNormalized)
    {
        // Arrange & Act
        var result1 = await _mockPassthroughClient.ExecuteAsync(args);
        var normalized1 = NormalizeOutput(_mockPassthroughClient.LastOutput!);

        var result2 = await _mockPassthroughClient.ExecuteAsync(args);
        var normalized2 = NormalizeOutput(_mockPassthroughClient.LastOutput!);

        // Assert - 複数回実行しても正規化後の出力が一致する（決定性）
        result1.ShouldBe(0);
        result2.ShouldBe(0);
        normalized1.ShouldBe(normalized2);
        normalized1.ShouldBe(expectedNormalized);
    }

    [Fact]
    public async Task ExecuteAsync_ExportRoundTrip_ShouldProduceDeterministicJsonNormalization()
    {
        // Arrange
        var args = new[] { "export" };

        // Act - 複数回実行
        await _mockPassthroughClient.ExecuteAsync(args);
        var normalized1 = NormalizeJsonOutput(_mockPassthroughClient.LastOutput!);

        await _mockPassthroughClient.ExecuteAsync(args);
        var normalized2 = NormalizeJsonOutput(_mockPassthroughClient.LastOutput!);

        // Assert - JSON正規化の決定性確認
        normalized1.ShouldBe(normalized2);

        // JSON構造の検証
        var jsonDoc = JsonDocument.Parse(normalized1);
        jsonDoc.RootElement.GetProperty("Sources").GetArrayLength().ShouldBeGreaterThan(0);
        jsonDoc.RootElement.GetProperty("WinGetVersion").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 出力を正規化して比較可能な形式に変換
    /// </summary>
    private static string NormalizeOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        // 1. 複数の空白を単一のパイプ区切りに正規化（テーブル形式対応）
        var normalized = Regex.Replace(output, @"\s{2,}", "|");

        // 2. 行末の空白を除去
        normalized = Regex.Replace(normalized, @"\s+$", "", RegexOptions.Multiline);

        // 3. 連続する改行を単一の改行に正規化
        normalized = Regex.Replace(normalized, @"\n{2,}", "\n");

        // 4. 文字列の前後の空白を除去
        return normalized.Trim();
    }

    /// <summary>
    /// JSON出力を正規化（インデント・ソート）
    /// </summary>
    private static string NormalizeJsonOutput(string jsonOutput)
    {
        if (string.IsNullOrWhiteSpace(jsonOutput))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(jsonOutput);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null // 元のプロパティ名を保持
            });
        }
        catch (JsonException)
        {
            // JSON以外の出力は通常の正規化を適用
            return NormalizeOutput(jsonOutput);
        }
    }
}