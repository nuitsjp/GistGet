using NuitsJp.GistGet.Models;
using Shouldly;

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
        var architecture = "x64";
        var scope = "user";
        var source = "winget";
        var custom = "--force";

        // Act
        var package = new PackageDefinition(packageId, version, null, architecture, scope, source, custom);

        // Assert
        package.Id.ShouldBe(packageId);
        package.Version.ShouldBe(version);
        package.Uninstall.ShouldBeNull();
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
        // Arrange & Act & Assert
        // 空のIDでコンストラクタを呼び出すとArgumentExceptionが発生する
        Should.Throw<ArgumentException>(() => new PackageDefinition(string.Empty));
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
        var package = new PackageDefinition("AkelPad.AkelPad", architecture: architecture);

        // Act & Assert
        Should.NotThrow(() => package.Validate());
    }

    [Fact]
    public void Validate_WithInvalidArchitecture_ShouldThrowArgumentException()
    {
        // Arrange
        var package = new PackageDefinition("AkelPad.AkelPad", architecture: "invalid");

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

    [Fact]
    public void Constructor_WithAllYamlSpecificationProperties_ShouldSetProperties()
    {
        // Arrange & Act
        var package = new PackageDefinition(
            id: "Microsoft.VisualStudioCode",
            version: "1.85.0",
            uninstall: true,
            allowHashMismatch: true,
            architecture: "x64",
            custom: "/VERYSILENT /NORESTART",
            force: true,
            header: "Authorization: Bearer token",
            installerType: "exe",
            locale: "en-US",
            location: "C:\\Program Files\\VS Code",
            log: "C:\\Temp\\install.log",
            mode: "silent",
            overrideArgs: "/SILENT",
            scope: "machine",
            skipDependencies: true,
            confirm: true,
            whatIf: false
        );

        // Assert
        package.Id.ShouldBe("Microsoft.VisualStudioCode");
        package.Version.ShouldBe("1.85.0");
        package.Uninstall.ShouldBe(true);
        package.AllowHashMismatch.ShouldBe(true);
        package.Architecture.ShouldBe("x64");
        package.Custom.ShouldBe("/VERYSILENT /NORESTART");
        package.Force.ShouldBe(true);
        package.Header.ShouldBe("Authorization: Bearer token");
        package.InstallerType.ShouldBe("exe");
        package.Locale.ShouldBe("en-US");
        package.Location.ShouldBe("C:\\Program Files\\VS Code");
        package.Log.ShouldBe("C:\\Temp\\install.log");
        package.Mode.ShouldBe("silent");
        package.Override.ShouldBe("/SILENT");
        package.Scope.ShouldBe("machine");
        package.SkipDependencies.ShouldBe(true);
        package.Confirm.ShouldBe(true);
        package.WhatIf.ShouldBe(false);
    }

    [Fact]
    public void UninstallProperty_ShouldSupportBooleanType()
    {
        // Arrange & Act
        var package1 = new PackageDefinition("Test.Package1", uninstall: true);
        var package2 = new PackageDefinition("Test.Package2", uninstall: false);

        // Assert
        package1.Uninstall.ShouldBe(true);
        package2.Uninstall.ShouldBe(false);
    }
}