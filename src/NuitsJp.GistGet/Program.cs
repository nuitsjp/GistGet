using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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
        
        // Build command line parser with custom configuration
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((exception, context) =>
            {
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogError(exception, "Unhandled exception occurred");
                Console.WriteLine($"Error: {exception.Message}");
                context.ExitCode = 1;
            })
            .Build();

        // Parse and execute command
        return await parser.InvokeAsync(args);
    }
}