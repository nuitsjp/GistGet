
using GistGet;
using GistGet.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddTransient<CommandRouter>();
services.AddTransient<IWinGetPassthroughRunner, WinGetPassthroughRunner>();

await services
    .BuildServiceProvider()
    .GetRequiredService<CommandRouter>()
    .RunAsync(args);
