// Polyfill type required for init-only setters on older target frameworks.

#pragma warning disable IDE0130
namespace GistGet.Properties;
#pragma warning restore IDE0130

/// <summary>
/// Provides the IsExternalInit type required for init-only setters.
/// </summary>
internal sealed class IsExternalInit;
