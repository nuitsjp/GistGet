using GistGet.Application.Services;
using GistGet.Infrastructure.Security;
using GistGet.Infrastructure.System;
using GistGet.Infrastructure.WinGet;
using GistGet.Presentation;
using System.CommandLine;
using System.Threading.Tasks;

namespace GistGet;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Infrastructure
        var credentialService = new CredentialService();
        var processRunner = new ProcessRunner();
        var winGetRepository = new WinGetRepository();
        var winGetExecutor = new WinGetExecutor(processRunner);

        // Application
        var authService = new AuthService(credentialService);
        var gistService = new GistService(authService);
        var packageService = new PackageService(winGetRepository, winGetExecutor);

        // Presentation
        var commandBuilder = new CliCommandBuilder(packageService, gistService, authService);
        var rootCommand = commandBuilder.Build();

        return await rootCommand.InvokeAsync(args);
    }
}
