using Microsoft.Extensions.DependencyInjection;

namespace NuitsJp.GistGet.Commands;

/// <summary>
/// Base class for all command handlers providing common functionality
/// </summary>
public abstract class BaseCommandHandler
{
    protected static IServiceProvider? ServiceProvider { get; set; }
    
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public abstract Task<int> ExecuteAsync();
}

