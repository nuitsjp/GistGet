using GistGet.Infrastructure.Diagnostics;
using GistGet.Model;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddTransient<IWinGetPassthroughRunner, WinGetPassthroughRunner>();

//await services
//    .BuildServiceProvider()
//    .GetRequiredService<RootCommand>()
//    .RunAsync(args);