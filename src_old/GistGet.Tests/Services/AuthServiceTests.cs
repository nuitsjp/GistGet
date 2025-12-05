using GistGet.Application.Services;
using GistGet.Infrastructure.Security;
using Moq;
using Xunit;

namespace GistGet.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task GetUserInfoAsync_NoToken_ReturnsNull()
    {
        // Arrange
        var mockCredentialService = new Mock<ICredentialService>();
        mockCredentialService.Setup(x => x.GetCredential(It.IsAny<string>()))
            .Returns((string?)null);

        var authService = new AuthService(mockCredentialService.Object);

        // Act
        var result = await authService.GetUserInfoAsync();

        // Assert
        Assert.Null(result);
    }
}
