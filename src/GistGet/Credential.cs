// Authentication credential model for GitHub access.

namespace GistGet;

/// <summary>
/// Represents the GitHub credential data used by the application.
/// </summary>
public record Credential(string Username, string Token);
