using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuitsJp.GistGet;
using NuitsJp.GistGet.Abstractions;
using NuitsJp.GistGet.Infrastructure;
using NuitsJp.GistGet.Services;

// 依存性注入コンテナの構築
var services = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    })
    // Core services
    .AddSingleton<ICommandService, CommandService>()
    .AddSingleton<IErrorMessageService, ErrorMessageService>()
    // WinGet clients
    .AddSingleton<IWinGetClient, WinGetComClient>()
    .AddSingleton<IWinGetPassthroughClient, WinGetPassthrough>()
    // Gist services
    .AddSingleton<IGistSyncService, GistSyncService>()
    // GitHub services
    .AddSingleton<IGitHubAuthService, GitHubAuthService>()
    .AddSingleton<AuthCommand>()
    .AddSingleton<TestGistCommand>()
    // Infrastructure
    .AddSingleton<IProcessWrapper, ProcessWrapper>()
    .BuildServiceProvider();

try
{
    var commandService = services.GetRequiredService<ICommandService>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    logger.LogDebug("Starting GistGet with args: {Args}", string.Join(" ", args));

    var exitCode = await commandService.ExecuteAsync(args);

    logger.LogDebug("GistGet completed with exit code: {ExitCode}", exitCode);
    return exitCode;
}
catch (Exception ex)
{
    var logger = services.GetService<ILogger<Program>>();
    logger?.LogError(ex, "Unhandled exception occurred: {ErrorMessage}", ex.Message);
    return 1;
}
finally
{
    await services.DisposeAsync();
}
