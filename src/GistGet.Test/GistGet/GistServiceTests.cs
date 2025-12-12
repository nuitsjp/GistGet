using Moq;
using Shouldly;

namespace GistGet;

public class GistServiceTests
{
    private readonly Mock<IWinGetPassthroughRunner> _passthroughRunnerMock;
    private readonly GistService _target;

    public GistServiceTests()
    {
        _passthroughRunnerMock = new Mock<IWinGetPassthroughRunner>();
        _target = new GistService(_passthroughRunnerMock.Object);
    }

    [Fact]
    public async Task RunPassthroughAsync_CallsRunner_WithCommandAndArgs()
    {
        // Arrange
        var command = "search";
        var args = new[] { "vscode", "--source", "winget" };
        var expectedArgs = new[] { "search", "vscode", "--source", "winget" };
        var expectedExitCode = 0;

        _passthroughRunnerMock.Setup(x => x.RunAsync(It.IsAny<string[]>()))
            .ReturnsAsync(expectedExitCode);

        // Act
        var result = await _target.RunPassthroughAsync(command, args);

        // Assert
        result.ShouldBe(expectedExitCode);
        _passthroughRunnerMock.Verify(x => x.RunAsync(
            It.Is<string[]>(a => a.SequenceEqual(expectedArgs))
        ), Times.Once);
    }
}
