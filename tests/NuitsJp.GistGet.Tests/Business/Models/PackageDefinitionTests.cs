using Shouldly;
using Xunit;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Tests.Business.Models;

public class PackageDefinitionTests
{
    [Fact]
    public void Constructor_WithValidId_ShouldInitializeCorrectly()
    {
        // Arrange
        var packageId = "AkelPad.AkelPad";

        // Act
        var package = new PackageDefinition(packageId);

        // Assert
        package.Id.ShouldBe(packageId);
        package.Version.ShouldBeNull();
        package.Uninstall.ShouldBeNull();
        package.Architecture.ShouldBeNull();
        package.Scope.ShouldBeNull();
        package.Source.ShouldBeNull();
        package.Custom.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new PackageDefinition(invalidId!));
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var packageId = "AkelPad.AkelPad";
        var version = "4.9.8";
        var uninstall = "--silent";
        var architecture = "x64";
        var scope = "user";
        var source = "winget";
        var custom = "--force";

        // Act
        var package = new PackageDefinition(packageId, version, uninstall, architecture, scope, source, custom);

        // Assert
        package.Id.ShouldBe(packageId);
        package.Version.ShouldBe(version);
        package.Uninstall.ShouldBe(uninstall);
        package.Architecture.ShouldBe(architecture);
        package.Scope.ShouldBe(scope);
        package.Source.ShouldBe(source);
        package.Custom.ShouldBe(custom);
    }

    [Fact]
    public void Validate_WithValidPackage_ShouldNotThrow()
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad");

        // Act & Assert
        Should.NotThrow(() => package.Validate());
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad");
        // リフレクションでプライベートプロパティを変更
        var idProperty = typeof(PackageDefinition).GetProperty("Id");
        idProperty?.SetValue(package, string.Empty);

        // Act & Assert
        Should.Throw<ArgumentException>(() => package.Validate());
    }

    [Theory]
    [InlineData("machine")]
    [InlineData("user")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithValidScope_ShouldNotThrow(string? scope)
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad")
        {
            Scope = scope
        };

        // Act & Assert
        Should.NotThrow(() => package.Validate());
    }

    [Fact]
    public void Validate_WithInvalidScope_ShouldThrowArgumentException()
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad")
        {
            Scope = "invalid"
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => package.Validate());
    }

    [Theory]
    [InlineData("x86")]
    [InlineData("x64")]
    [InlineData("arm")]
    [InlineData("arm64")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithValidArchitecture_ShouldNotThrow(string? architecture)
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad")
        {
            Architecture = architecture
        };

        // Act & Assert
        Should.NotThrow(() => package.Validate());
    }

    [Fact]
    public void Validate_WithInvalidArchitecture_ShouldThrowArgumentException()
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad")
        {
            Architecture = "invalid"
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => package.Validate());
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var package1 = new PackageDefinition("AkelPad.AkelPad");
        var package2 = new PackageDefinition("AkelPad.AkelPad");

        // Act & Assert
        package1.Equals(package2).ShouldBeTrue();
        (package1 == package2).ShouldBeTrue();
        (package1 != package2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageDefinition("AkelPad.AkelPad");
        var package2 = new PackageDefinition("Microsoft.VisualStudioCode");

        // Act & Assert
        package1.Equals(package2).ShouldBeFalse();
        (package1 == package2).ShouldBeFalse();
        (package1 != package2).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var package1 = new PackageDefinition("AkelPad.AkelPad");
        var package2 = new PackageDefinition("AkelPad.AkelPad");

        // Act & Assert
        package1.GetHashCode().ShouldBe(package2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnPackageId()
    {
        // Arrange
        var packageId = "AkelPad.AkelPad";
        var package = new PackageDefinition(packageId);

        // Act
        var result = package.ToString();

        // Assert
        result.ShouldBe(packageId);
    }

    [Fact]
    public void CompareTo_ShouldSortByIdAlphabetically()
    {
        // Arrange
        var package1 = new PackageDefinition("AkelPad.AkelPad");
        var package2 = new PackageDefinition("Microsoft.VisualStudioCode");
        var package3 = new PackageDefinition("Zoom.Zoom");

        var packages = new List<PackageDefinition> { package3, package1, package2 };

        // Act
        packages.Sort();

        // Assert
        packages[0].Id.ShouldBe("AkelPad.AkelPad");
        packages[1].Id.ShouldBe("Microsoft.VisualStudioCode");
        packages[2].Id.ShouldBe("Zoom.Zoom");
    }
}