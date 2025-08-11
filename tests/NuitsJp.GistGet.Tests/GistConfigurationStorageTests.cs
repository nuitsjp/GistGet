using Shouldly;
using Xunit;
using NuitsJp.GistGet.Models;
using NuitsJp.GistGet.Services;

namespace NuitsJp.GistGet.Tests;

public class GistConfigurationStorageTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly GistConfigurationStorage _storage;

    public GistConfigurationStorageTests()
    {
        // テスト用の一時ディレクトリを作成
        _testDirectory = Path.Combine(Path.GetTempPath(), $"GistGetTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _storage = new GistConfigurationStorage(_testDirectory);
    }

    public void Dispose()
    {
        // テスト後のクリーンアップ
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SaveGistConfigurationAsync_ShouldCreateFileAndEncryptContent()
    {
        // Arrange
        var config = new GistConfiguration("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml");

        // Act
        await _storage.SaveGistConfigurationAsync(config);

        // Assert
        var filePath = Path.Combine(_testDirectory, "gist.dat");
        File.Exists(filePath).ShouldBeTrue();

        // ファイルの内容が暗号化されていることを確認（JSONとして読めないこと）
        var encryptedContent = await File.ReadAllBytesAsync(filePath);
        encryptedContent.Length.ShouldBeGreaterThan(0);

        // 暗号化されたバイト配列をJSON文字列として解釈できないことを確認
        Should.Throw<Exception>(() =>
        {
            var jsonString = System.Text.Encoding.UTF8.GetString(encryptedContent);
            GistConfiguration.FromJson(jsonString);
        });
    }

    [Fact]
    public async Task LoadGistConfigurationAsync_ShouldDecryptAndReturnConfiguration()
    {
        // Arrange
        var originalConfig = new GistConfiguration("d239aabb67e60650fbcb2b20a8342be1", "packages.yaml");
        await _storage.SaveGistConfigurationAsync(originalConfig);

        // Act
        var loadedConfig = await _storage.LoadGistConfigurationAsync();

        // Assert
        loadedConfig.ShouldNotBeNull();
        loadedConfig.GistId.ShouldBe(originalConfig.GistId);
        loadedConfig.FileName.ShouldBe(originalConfig.FileName);
        loadedConfig.CreatedAt.ShouldBeInRange(originalConfig.CreatedAt.AddMilliseconds(-1), originalConfig.CreatedAt.AddMilliseconds(1));
        loadedConfig.LastAccessedAt.ShouldBeInRange(originalConfig.LastAccessedAt.AddMilliseconds(-1), originalConfig.LastAccessedAt.AddMilliseconds(1));
    }

    [Fact]
    public async Task LoadGistConfigurationAsync_WhenFileNotExists_ShouldReturnNull()
    {
        // Act
        var config = await _storage.LoadGistConfigurationAsync();

        // Assert
        config.ShouldBeNull();
    }

    [Fact]
    public async Task SaveGistConfigurationAsync_WhenDirectoryNotExists_ShouldCreateDirectory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");
        var storage = new GistConfigurationStorage(nonExistentDir);
        var config = new GistConfiguration("abc123", "packages.yaml");

        // Act
        await storage.SaveGistConfigurationAsync(config);

        // Assert
        Directory.Exists(nonExistentDir).ShouldBeTrue();
        var filePath = Path.Combine(nonExistentDir, "gist.dat");
        File.Exists(filePath).ShouldBeTrue();
    }

    [Fact]
    public async Task IsConfiguredAsync_WhenFileExists_ShouldReturnTrue()
    {
        // Arrange
        var config = new GistConfiguration("abc123", "packages.yaml");
        await _storage.SaveGistConfigurationAsync(config);

        // Act
        var isConfigured = await _storage.IsConfiguredAsync();

        // Assert
        isConfigured.ShouldBeTrue();
    }

    [Fact]
    public async Task IsConfiguredAsync_WhenFileNotExists_ShouldReturnFalse()
    {
        // Act
        var isConfigured = await _storage.IsConfiguredAsync();

        // Assert
        isConfigured.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithValidPath_ShouldSetCorrectFilePath()
    {
        // Act
        var storage = new GistConfigurationStorage(_testDirectory);

        // Assert
        storage.FilePath.ShouldBe(Path.Combine(_testDirectory, "gist.dat"));
    }

    [Fact]
    public async Task SaveAndLoad_MultipleTimes_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var config1 = new GistConfiguration("abc123", "packages.yaml");
        var config2 = new GistConfiguration("def456", "apps.yaml");

        // Act & Assert - 最初の保存と読み込み
        await _storage.SaveGistConfigurationAsync(config1);
        var loaded1 = await _storage.LoadGistConfigurationAsync();
        loaded1!.GistId.ShouldBe(config1.GistId);
        loaded1.FileName.ShouldBe(config1.FileName);

        // Act & Assert - 上書き保存と読み込み
        await _storage.SaveGistConfigurationAsync(config2);
        var loaded2 = await _storage.LoadGistConfigurationAsync();
        loaded2!.GistId.ShouldBe(config2.GistId);
        loaded2.FileName.ShouldBe(config2.FileName);
    }
}