using Shouldly;
using Xunit;

namespace GistGet.Infrastructure.Security;

public class CredentialServiceTests : IDisposable
{
    protected readonly CredentialService _sut = new();
    protected readonly string TestTargetName = $"GistGet.Test.Credential.{Guid.NewGuid()}";

    public void Dispose()
    {
        _sut.DeleteCredential(TestTargetName);
    }

    public class SaveCredential : CredentialServiceTests
    {
        [Fact]
        public void ValidCredential_ShouldPersist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var username = "testuser";
            var password = "testpassword";

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.SaveCredential(TestTargetName, username, password);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            
            // Verify persistence
            _sut.TryGetCredential(TestTargetName, out var retrieved).ShouldBeTrue();
            retrieved.ShouldBe(password);
        }
    }

    public class TryGetCredential : CredentialServiceTests
    {
        [Fact]
        public void ExistingCredential_ShouldRetrieve()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var password = "storedPassword";
            _sut.SaveCredential(TestTargetName, "user", password);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.TryGetCredential(TestTargetName, out var retrievedPassword);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            retrievedPassword.ShouldBe(password);
        }

        [Fact]
        public void NonExistentCredential_ShouldReturnFalse()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _sut.DeleteCredential(TestTargetName);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.TryGetCredential(TestTargetName, out var retrievedPassword);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeFalse();
            retrievedPassword.ShouldBeNull();
        }
    }

    public class DeleteCredential : CredentialServiceTests
    {
        [Fact]
        public void ExistingCredential_ShouldRemove()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _sut.SaveCredential(TestTargetName, "user", "pass");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.DeleteCredential(TestTargetName);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
            _sut.TryGetCredential(TestTargetName, out _).ShouldBeFalse();
        }

        [Fact]
        public void NonExistentCredential_ShouldReturnTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            _sut.DeleteCredential(TestTargetName);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = _sut.DeleteCredential(TestTargetName);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Based on code analysis, NOT_FOUND (1168) allows logic to return true?
            // Checking logic: if error != 1168 return false. So if error IS 1168, it continues and returns true.
            result.ShouldBeTrue();
        }
    }
}