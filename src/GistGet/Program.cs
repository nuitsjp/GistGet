using System.CommandLine;
using GistGet;
using GistGet.Infrastructure.GitHub;
using GistGet.Infrastructure.Security;
using GistGet.Presentation;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

// GistGet
services.AddTransient<IAuthService, AuthService>();
services.AddTransient<IPackageService, PackageService>();
services.AddTransient<IGistService, GistService>();
services.AddTransient<IGistGetService, GistGetService>();

// Presentation
services.AddTransient<CommandBuilder>();
services.AddTransient<IConsoleService, ConsoleService>();

// Infrastructure
services.AddTransient<ICredentialService, CredentialService>();

await services
    .BuildServiceProvider()
    .GetRequiredService<CommandBuilder>()
    .Build()
    .InvokeAsync(args);