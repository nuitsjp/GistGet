using Shouldly;
using Xunit;

namespace GistGet.Infrastructure;

[Collection("CredentialTests")]

public class CredentialServiceTests : IDisposable
{
    protected readonly string TestTargetName = $"GistGet.Test.Credential.{Guid.NewGuid()}";
    protected readonly CredentialService _sut;

    public CredentialServiceTests()
    {
        _sut = new CredentialService(TestTargetName);
    }

    public void Dispose()
    {
        _sut.DeleteCredential();
    }

    [Collection("CredentialTests")]
    public class SaveCredential : CredentialServiceTests
    {
        [Fact]
        public void ValidCredential_ShouldPersist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var username = "testuser";
            var token = "testpassword"; // Renamed variable to avoid confusion, though keeping value string

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.SaveCredential(new Credential(username, token));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            
            // Verify persistence
            _sut.TryGetCredential(out var retrieved).ShouldBeTrue();
            retrieved.ShouldNotBeNull();
            retrieved.Username.ShouldBe(username);
            retrieved.Token.ShouldBe(token);
        }
    }

    [Collection("CredentialTests")]
    public class TryGetCredential : CredentialServiceTests
    {
        [Fact]
        public void ExistingCredential_ShouldRetrieve()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var token = "storedPassword";
            var username = "user";
            _sut.SaveCredential(new Credential(username, token)).ShouldBeTrue();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.TryGetCredential(out var credential);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            credential.ShouldNotBeNull();
            credential.Username.ShouldBe(username);
            credential.Token.ShouldBe(token);
        }

        [Fact]
        public void NonExistentCredential_ShouldReturnFalse()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.TryGetCredential(out var credential);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeFalse();
            credential.ShouldBeNull();
        }
    }

    [Collection("CredentialTests")]
    public class DeleteCredential : CredentialServiceTests
    {
        [Fact]
        public void ExistingCredential_ShouldRemove()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _sut.SaveCredential(new Credential("user", "pass")).ShouldBeTrue();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            _sut.TryGetCredential(out _).ShouldBeFalse();
        }

        [Fact]
        public void NonExistentCredential_ShouldReturnTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Based on code analysis, NOT_FOUND (1168) allows logic to return true?
            // Checking logic: if error != 1168 return false. So if error IS 1168, it continues and returns true.
            result.ShouldBeTrue();
        }
    }
}