using NuitsJp.GistGet.Commands;
using Shouldly;

namespace NuitsJp.GistGet.Test.Commands;

/// <summary>
/// Common tests that every command handler must satisfy.
/// Inherit this in each concrete handler test to enforce consistency.
/// </summary>
public abstract class CommandHandlerTestsBase<THandler>
    where THandler : BaseCommandHandler, new()
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_Zero()
    {
        var handler = new THandler();
        var result = await handler.ExecuteAsync();
        result.ShouldBe(0);
    }

    [Fact]
    public void Should_Inherit_From_BaseCommandHandler()
    {
        typeof(BaseCommandHandler).IsAssignableFrom(typeof(THandler)).ShouldBeTrue();
    }
}

