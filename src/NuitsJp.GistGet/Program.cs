using Microsoft.Extensions.Logging;
using NuitsJp.GistGet;

var host = AppHost.CreateHost();
var logger = host.Services.GetService(typeof(ILogger<Program>)) as ILogger<Program>;

try
{
    logger?.LogDebug("Starting GistGet with args: {Args}", string.Join(" ", args));

    var app = new RunnerApplication();
    var exitCode = await app.RunAsync(host, args);

    logger?.LogDebug("GistGet completed with exit code: {ExitCode}", exitCode);
    return exitCode;
}
catch (Exception ex)
{
    logger?.LogError(ex, "Unhandled exception occurred: {ErrorMessage}", ex.Message);
    return 1;
}
finally
{
    host.Dispose();
}
