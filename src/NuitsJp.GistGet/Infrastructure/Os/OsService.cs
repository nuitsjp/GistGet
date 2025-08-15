using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Infrastructure.Os;

/// <summary>
/// オペレーティングシステム操作の実装クラス
/// Windowsのshutdownコマンドを使用した実際のシステム操作
/// </summary>
public class OsService(ILogger<OsService> logger) : IOsService
{
    private readonly ILogger<OsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// システム再起動を即座に実行
    /// </summary>
    public async Task ExecuteRebootAsync()
    {
        await ExecuteRebootAsync(0);
    }

    /// <summary>
    /// システム再起動を指定時間後に実行
    /// </summary>
    /// <param name="delaySeconds">再起動までの遅延時間（秒）</param>
    public async Task ExecuteRebootAsync(int delaySeconds)
    {
        try
        {
            _logger.LogInformation("Executing system reboot with {Delay} seconds delay", delaySeconds);

            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = $"/r /t {delaySeconds}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    _logger.LogInformation("System reboot command executed successfully");
                }
                else
                {
                    var stderr = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("System reboot command failed with exit code {ExitCode}: {Error}",
                        exitCode, stderr);
                    throw new InvalidOperationException($"Reboot command failed with exit code {exitCode}: {stderr}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute system reboot: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// システムシャットダウンを実行
    /// </summary>
    public async Task ExecuteShutdownAsync()
    {
        try
        {
            _logger.LogInformation("Executing system shutdown");

            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/s /t 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    _logger.LogInformation("System shutdown command executed successfully");
                }
                else
                {
                    var stderr = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("System shutdown command failed with exit code {ExitCode}: {Error}",
                        exitCode, stderr);
                    throw new InvalidOperationException($"Shutdown command failed with exit code {exitCode}: {stderr}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute system shutdown: {Message}", ex.Message);
            throw;
        }
    }
}