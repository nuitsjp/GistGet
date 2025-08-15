using System.Security.Cryptography;
using System.Text;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Infrastructure.Storage;

public class GistConfigurationStorage : IGistConfigurationStorage
{
    private static readonly SemaphoreSlim _fileSemaphore = new(1, 1);

    public GistConfigurationStorage(string appDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(appDataDirectory))
            throw new ArgumentException("App data directory cannot be null or empty", nameof(appDataDirectory));

        FilePath = Path.Combine(appDataDirectory, "gist.dat");
    }

    public string FilePath { get; }

    public async Task SaveGistConfigurationAsync(GistConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        configuration.Validate();

        await _fileSemaphore.WaitAsync();
        try
        {
            // ディレクトリが存在しない場合は作成
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // JSON文字列に変換
            var json = configuration.ToJson();
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            // DPAPI で暗号化（CurrentUser スコープ）
            var encryptedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);

            // ファイルに保存
            await File.WriteAllBytesAsync(FilePath, encryptedBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException($"Failed to encrypt configuration: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to save configuration file: {ex.Message}", ex);
        }
        // UnauthorizedAccessException はそのまま投げる
        finally
        {
            _fileSemaphore.Release();
        }
    }

    public async Task<GistConfiguration?> LoadGistConfigurationAsync()
    {
        if (!File.Exists(FilePath))
            return null;

        await _fileSemaphore.WaitAsync();
        try
        {
            // 暗号化されたファイルを読み込み
            var encryptedBytes = await File.ReadAllBytesAsync(FilePath);

            // DPAPI で復号化
            var jsonBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(jsonBytes);

            // JSON から GistConfiguration オブジェクトに変換
            return GistConfiguration.FromJson(json);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                $"Failed to decrypt configuration. The file may be corrupted or created by a different user: {ex.Message}",
                ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to read configuration file: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Access denied when reading configuration: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Invalid configuration data: {ex.Message}", ex);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        try
        {
            var config = await LoadGistConfigurationAsync();
            return config != null;
        }
        catch
        {
            // ファイルが破損している場合や読み取れない場合は未設定として扱う
            return false;
        }
    }

    public async Task DeleteConfigurationAsync()
    {
        await _fileSemaphore.WaitAsync();
        try
        {
            if (File.Exists(FilePath))
                try
                {
                    File.Delete(FilePath);
                }
                catch (IOException ex)
                {
                    throw new InvalidOperationException($"Failed to delete configuration file: {ex.Message}", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new InvalidOperationException($"Access denied when deleting configuration: {ex.Message}", ex);
                }
        }
        finally
        {
            _fileSemaphore.Release();
        }

        await Task.CompletedTask;
    }

    public static GistConfigurationStorage CreateDefault()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var gistGetDir = Path.Combine(appDataPath, "GistGet");
        return new GistConfigurationStorage(gistGetDir);
    }
}