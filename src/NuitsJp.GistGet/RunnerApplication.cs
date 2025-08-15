
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Presentation;

namespace NuitsJp.GistGet;

/// <summary>
/// 実行エントリのアプリケーション。DI に依存しつつもテスト可能な形に分離。
/// </summary>
public class RunnerApplication
{
    public async Task<int> RunAsync(IHost host, string[] args)
    {
        var logger = host.Services.GetService<ILogger<RunnerApplication>>();

        try
        {
            var commandService = host.Services.GetRequiredService<ICommandRouter>();

            logger?.LogDebug("Starting GistGet with args: {Args}", string.Join(" ", args));

            var exitCode = await commandService.ExecuteAsync(args);

            logger?.LogDebug("GistGet completed with exit code: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled exception occurred: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}
