using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet;
using NuitsJp.GistGet.Abstractions;

namespace NuitsJp.GistGet.Tests;

public class RunnerApplicationTests
{
    [Fact]
    public async Task RunAsync_UsesCommandService_ReturnsExitCode()
    {
        // Arrange
        var commandService = new Mock<ICommandService>();
        commandService.Setup(x => x.ExecuteAsync(It.IsAny<string[]>())).ReturnsAsync(0);

        var services = new ServiceCollection()
            .AddLogging(b => b.AddDebug())
            .AddSingleton<ICommandService>(commandService.Object)
            .BuildServiceProvider();

        var host = Mock.Of<IHost>(h => h.Services == services);
        var app = new RunnerApplication();

        // Act
        var code = await app.RunAsync(host, new[] { "--help" });

        // Assert
        Assert.Equal(0, code);
        commandService.Verify(x => x.ExecuteAsync(It.IsAny<string[]>()), Times.Once);
    }
}
