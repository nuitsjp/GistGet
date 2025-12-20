// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantTypeDeclarationBody
namespace GistGet.Test;

[CollectionDefinition("Console redirection", DisableParallelization = true)]
public class ConsoleRedirectionCollectionDefinition : ICollectionFixture<ConsoleRedirectionFixture>
{
}

public class ConsoleRedirectionFixture
{
}
