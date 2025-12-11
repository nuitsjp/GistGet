using System.CommandLine;
using GistGet;
using GistGet.Infrastructure.GitHub;
using GistGet.Presentation;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

// GistGet
services.AddTransient<IAuthService, AuthService>();
services.AddTransient<IPackageService, PackageService>();
services.AddTransient<IGistService, GistService>();

// Presentation
services.AddTransient<CommandBuilder>();

await services
    .BuildServiceProvider()
    .GetRequiredService<CommandBuilder>()
    .Build()
    .InvokeAsync(args);