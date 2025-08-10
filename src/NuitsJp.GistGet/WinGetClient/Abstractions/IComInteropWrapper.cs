using Microsoft.Management.Deployment;

namespace NuitsJp.GistGet.WinGetClient.Abstractions;

/// <summary>
/// Abstraction layer for WinGet COM API interop operations
/// Enables unit testing with mocked COM interactions
/// </summary>
public interface IComInteropWrapper
{
    /// <summary>
    /// Creates a new PackageManager instance
    /// </summary>
    /// <returns>PackageManager instance or null if creation fails</returns>
    PackageManager? CreatePackageManager();

    /// <summary>
    /// Checks if COM API is available and functional
    /// </summary>
    /// <returns>True if COM API is available</returns>
    bool IsComApiAvailable();
}
