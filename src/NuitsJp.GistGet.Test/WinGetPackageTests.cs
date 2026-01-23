using Shouldly;

namespace NuitsJp.GistGet.Test;

public class WinGetPackageTests
{
    public class IsVersionUnknown : WinGetPackageTests
    {
        [Fact]
        public void WhenVersionIsEmptyString_ReturnsTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new WinGetPackage(
                Name: "Test App",
                Id: new PackageId("Test.App"),
                Version: new Version(""),
                UsableVersion: null,
                Source: null);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.IsVersionUnknown;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
        }

        [Fact]
        public void WhenVersionIsUnknown_ReturnsTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new WinGetPackage(
                Name: "Test App",
                Id: new PackageId("Test.App"),
                Version: new Version("Unknown"),
                UsableVersion: null,
                Source: null);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.IsVersionUnknown;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
        }

        [Fact]
        public void WhenVersionIsUnknownLowerCase_ReturnsTrue()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new WinGetPackage(
                Name: "Test App",
                Id: new PackageId("Test.App"),
                Version: new Version("unknown"),
                UsableVersion: null,
                Source: null);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.IsVersionUnknown;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeTrue();
        }

        [Fact]
        public void WhenVersionIsValidVersion_ReturnsFalse()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new WinGetPackage(
                Name: "Test App",
                Id: new PackageId("Test.App"),
                Version: new Version("1.0.0"),
                UsableVersion: null,
                Source: null);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.IsVersionUnknown;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeFalse();
        }
    }
}
