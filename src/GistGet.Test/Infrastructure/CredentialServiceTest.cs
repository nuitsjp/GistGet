using Shouldly;

namespace GistGet.Infrastructure;

[Collection("CredentialTests")]

public class CredentialServiceTests : IDisposable
{
    protected readonly string TestTargetName = $"GistGet.Test.Credential.{Guid.NewGuid()}";
    protected readonly CredentialService Sut;

    public CredentialServiceTests()
    {
        Sut = new CredentialService(TestTargetName);
    }

    public void Dispose()
    {
        Sut.DeleteCredential();
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
            var result = Sut.SaveCredential(new Credential(username, token));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();

            // Verify persistence
            Sut.TryGetCredential(out var retrieved).ShouldBeTrue();
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
            Sut.SaveCredential(new Credential(username, token)).ShouldBeTrue();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = Sut.TryGetCredential(out var credential);

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
            Sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = Sut.TryGetCredential(out var credential);

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
            Sut.SaveCredential(new Credential("user", "pass")).ShouldBeTrue();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = Sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            Sut.TryGetCredential(out _).ShouldBeFalse();
        }

        [Fact]
        public void NonExistentCredential_ShouldReturnTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            Sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = Sut.DeleteCredential();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Based on code analysis, NOT_FOUND (1168) allows logic to return true?
            // Checking logic: if error != 1168 return false. So if error IS 1168, it continues and returns true.
            result.ShouldBeTrue();
        }
    }
}
