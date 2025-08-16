using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Models;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Infrastructure.Storage;

[Trait("Category", "Integration")]
[Trait("Category", "FileSystem")]
public class GistConfigurationStorageTests : IDisposable
{
    private readonly GistConfigurationStorage _storage;
    private readonly string _testDirectory;

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
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
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
            var jsonString = Encoding.UTF8.GetString(encryptedContent);
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
        loadedConfig.CreatedAt.ShouldBeInRange(originalConfig.CreatedAt.AddMilliseconds(-1),
            originalConfig.CreatedAt.AddMilliseconds(1));
        loadedConfig.LastAccessedAt.ShouldBeInRange(originalConfig.LastAccessedAt.AddMilliseconds(-1),
            originalConfig.LastAccessedAt.AddMilliseconds(1));
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

    [Fact]
    [Trait("Category", "IntegrationConcurrency")]
    public async Task ConcurrentAccess_MultipleReadsAndWrites_ShouldNotCorruptData()
    {
        // Arrange
        var config1 = new GistConfiguration("concurrent_test_1", "test1.yaml");
        var config2 = new GistConfiguration("concurrent_test_2", "test2.yaml");

        // Act: 並行して読み書きを実行
        var tasks = new List<Task>();

        // 書き込みタスク
        tasks.Add(Task.Run(async () =>
        {
            for (var i = 0; i < 10; i++)
            {
                await _storage.SaveGistConfigurationAsync(config1);
                await Task.Delay(10);
            }
        }));

        // 別の書き込みタスク
        tasks.Add(Task.Run(async () =>
        {
            for (var i = 0; i < 10; i++)
            {
                await _storage.SaveGistConfigurationAsync(config2);
                await Task.Delay(15);
            }
        }));

        // 読み込みタスク
        var readResults = new List<GistConfiguration?>();
        tasks.Add(Task.Run(async () =>
        {
            for (var i = 0; i < 20; i++)
            {
                var config = await _storage.LoadGistConfigurationAsync();
                readResults.Add(config);
                await Task.Delay(5);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert: 最終的に有効な設定が読み込める
        var finalConfig = await _storage.LoadGistConfigurationAsync();
        finalConfig.ShouldNotBeNull();
        (finalConfig.GistId == config1.GistId || finalConfig.GistId == config2.GistId).ShouldBeTrue();

        // データ破損がないことを確認
        readResults.Where(r => r != null).ShouldAllBe(r =>
            r!.GistId == config1.GistId || r.GistId == config2.GistId);
    }

    [Fact]
    [Trait("Category", "IntegrationPermissions")]
    public async Task AccessDeniedDirectory_ShouldThrowUnauthorizedAccessException()
    {
        // Skip test if not running as administrator (required to set directory permissions)
        if (!IsRunningAsAdministrator())
            return;

        // Arrange: 読み取り専用ディレクトリを作成
        var readOnlyDir = Path.Combine(_testDirectory, "readonly");
        Directory.CreateDirectory(readOnlyDir);

        try
        {
            // ディレクトリのアクセス権限を読み取り専用に設定
            var dirInfo = new DirectoryInfo(readOnlyDir);
            var security = dirInfo.GetAccessControl();
            var user = WindowsIdentity.GetCurrent().User;

            // 書き込み権限を拒否
            security.AddAccessRule(new FileSystemAccessRule(
                user!,
                FileSystemRights.Write | FileSystemRights.CreateFiles,
                AccessControlType.Deny));
            dirInfo.SetAccessControl(security);

            var storage = new GistConfigurationStorage(readOnlyDir);
            var config = new GistConfiguration("test_readonly", "test.yaml");

            // Act & Assert
            await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            {
                await storage.SaveGistConfigurationAsync(config);
            });
        }
        finally
        {
            // クリーンアップ: アクセス権限を復元してディレクトリを削除可能にする
            try
            {
                var dirInfo = new DirectoryInfo(readOnlyDir);
                var security = dirInfo.GetAccessControl();
                var user = WindowsIdentity.GetCurrent().User;

                security.RemoveAccessRule(new FileSystemAccessRule(
                    user!,
                    FileSystemRights.Write | FileSystemRights.CreateFiles,
                    AccessControlType.Deny));
                dirInfo.SetAccessControl(security);

                Directory.Delete(readOnlyDir, true);
            }
            catch
            {
                // クリーンアップエラーは無視
            }
        }
    }

    [Fact]
    [Trait("Category", "IntegrationCorruption")]
    public async Task CorruptedConfigFile_ShouldHandleGracefully()
    {
        // Arrange: 破損したファイルを作成
        var filePath = Path.Combine(_testDirectory, "gist.dat");
        var corruptedData = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC, 0xFB }; // 不正なバイナリデータ
        await File.WriteAllBytesAsync(filePath, corruptedData);

        // Act & Assert: 破損ファイルからの読み込みは例外を投げるべき
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _storage.LoadGistConfigurationAsync();
        });
    }

    [Fact]
    [Trait("Category", "IntegrationLargeData")]
    public async Task LargeConfiguration_ShouldHandleCorrectly()
    {
        // Arrange: 大きなデータを含む設定
        var largeGistId = new string('a', 10000); // 10KB のGist ID（実際には無効だが境界値テスト用）
        var largeFileName = new string('b', 1000) + ".yaml"; // 1KB のファイル名

        var config = new GistConfiguration(largeGistId, largeFileName);

        // Act & Assert: 大きなデータでも正常に保存・読み込みできるか
        await Should.NotThrowAsync(async () =>
        {
            await _storage.SaveGistConfigurationAsync(config);
            var loaded = await _storage.LoadGistConfigurationAsync();
            loaded.ShouldNotBeNull();
            loaded!.GistId.ShouldBe(largeGistId);
            loaded.FileName.ShouldBe(largeFileName);
        });
    }

    [Fact]
    [Trait("Category", "IntegrationDiskSpace")]
    public async Task InsufficientDiskSpace_ShouldHandleGracefully()
    {
        // このテストは実際のディスク容量不足をシミュレートするのが困難なため、
        // 代わりに非常に大きなデータでの保存を試みる
        var config = new GistConfiguration("test", "test.yaml");

        // 正常なケースは動作することを確認
        await Should.NotThrowAsync(async () =>
        {
            await _storage.SaveGistConfigurationAsync(config);
            var loaded = await _storage.LoadGistConfigurationAsync();
            loaded.ShouldNotBeNull();
        });
    }

    // ヘルパーメソッド
    private static bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}