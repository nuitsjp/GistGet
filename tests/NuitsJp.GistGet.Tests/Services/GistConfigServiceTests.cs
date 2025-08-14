using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Tests.Services
{
    public class GistConfigServiceTests
    {
        [Fact]
        public async Task ConfigureGistAsync_ShouldReturnSuccess_WhenValidInputProvided()
        {
            // Arrange
            var mockAuthService = new Mock<IGitHubAuthService>();
            var mockStorage = new Mock<IGistConfigurationStorage>();
            var mockGistManager = new Mock<IGistManager>();
            var mockLogger = new Mock<ILogger<GistConfigService>>();

            mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);
            mockGistManager.Setup(x => x.ValidateGistAccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var service = new GistConfigService(
                mockAuthService.Object,
                mockStorage.Object,
                mockGistManager.Object,
                mockLogger.Object);

            var request = new GistConfigRequest
            {
                GistId = "test-gist-id",
                FileName = "packages.yaml"
            };

            // Act
            var result = await service.ConfigureGistAsync(request);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.GistId.ShouldBe("test-gist-id");
            result.FileName.ShouldBe("packages.yaml");
        }

        [Fact]
        public async Task ConfigureGistAsync_ShouldReturnFailure_WhenNotAuthenticated()
        {
            // Arrange
            var mockAuthService = new Mock<IGitHubAuthService>();
            var mockStorage = new Mock<IGistConfigurationStorage>();
            var mockGistManager = new Mock<IGistManager>();
            var mockLogger = new Mock<ILogger<GistConfigService>>();

            mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);

            var service = new GistConfigService(
                mockAuthService.Object,
                mockStorage.Object,
                mockGistManager.Object,
                mockLogger.Object);

            var request = new GistConfigRequest
            {
                GistId = "test-gist-id",
                FileName = "packages.yaml"
            };

            // Act
            var result = await service.ConfigureGistAsync(request);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage!.ShouldContain("認証");
        }
    }
}