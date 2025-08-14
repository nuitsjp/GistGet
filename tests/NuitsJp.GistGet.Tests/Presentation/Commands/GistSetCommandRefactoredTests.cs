using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using NuitsJp.GistGet.Presentation.Commands;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Business.Models;

namespace NuitsJp.GistGet.Tests.Presentation.Commands
{
    public class GistSetCommandRefactoredTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldUseGistConfigService_WhenCalled()
        {
            // Arrange
            var mockGistConfigService = new Mock<IGistConfigService>();
            var mockLogger = new Mock<ILogger<GistSetCommand>>();

            mockGistConfigService
                .Setup(x => x.ConfigureGistAsync(It.IsAny<GistConfigRequest>()))
                .ReturnsAsync(GistConfigResult.Success("test-gist-id", "packages.yaml"));

            var command = new GistSetCommand(
                mockGistConfigService.Object,
                mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync("test-gist-id", "packages.yaml");

            // Assert
            result.ShouldBe(0);
            mockGistConfigService.Verify(x => x.ConfigureGistAsync(
                It.Is<GistConfigRequest>(r =>
                    r.GistId == "test-gist-id" &&
                    r.FileName == "packages.yaml")),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnError_WhenServiceFails()
        {
            // Arrange
            var mockGistConfigService = new Mock<IGistConfigService>();
            var mockLogger = new Mock<ILogger<GistSetCommand>>();

            mockGistConfigService
                .Setup(x => x.ConfigureGistAsync(It.IsAny<GistConfigRequest>()))
                .ReturnsAsync(GistConfigResult.Failure("認証エラー"));

            var command = new GistSetCommand(
                mockGistConfigService.Object,
                mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync("test-gist-id", "packages.yaml");

            // Assert
            result.ShouldBe(1);
        }
    }
}