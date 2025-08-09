using System.CommandLine;
using NuitsJp.GistGet.ArgumentParser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet;

/// <summary>
/// Main entry point for GistGet .NET 8 application
/// Provides WinGet-compatible command-line interface
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create host builder for dependency injection
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IWinGetArgumentParser, WinGetArgumentParser>();
                services.AddSingleton<NuitsJp.GistGet.WinGetClient.IWinGetClient, NuitsJp.GistGet.WinGetClient.WinGetComClient>();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });

        using var host = hostBuilder.Build();
        
        // Set service provider for command handlers
        NuitsJp.GistGet.Commands.BaseCommandHandler.SetServiceProvider(host.Services);
        
        // Get argument parser from DI container
        var argumentParser = host.Services.GetRequiredService<IWinGetArgumentParser>();
        
        // Create root command
    var rootCommand = argumentParser.BuildRootCommand();

        // v2: CommandLineConfiguration を使って実行
        try
        {
            var config = new CommandLineConfiguration(rootCommand);
            var parseResult = rootCommand.Parse(args, config);
            return await parseResult.InvokeAsync();
        }
        catch (Exception exception)
        {
            var logger = host.Services.GetService<ILogger<Program>>();
            logger?.LogError(exception, "Unhandled exception occurred");
            Console.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }
}