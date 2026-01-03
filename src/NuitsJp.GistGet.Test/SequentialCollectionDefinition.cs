namespace NuitsJp.GistGet.Test;

/// <summary>
/// Defines a test collection that runs tests sequentially to avoid concurrency issues.
/// Used for tests that perform dotnet publish operations or have timing-sensitive mock verifications.
/// </summary>
[CollectionDefinition(SequentialCollectionDefinition.CollectionName, DisableParallelization = true)]
public class SequentialCollectionDefinition
{
    public const string CollectionName = "Sequential";
}
