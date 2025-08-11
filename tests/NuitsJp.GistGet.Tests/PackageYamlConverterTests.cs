using Shouldly;
using Xunit;
using NuitsJp.GistGet.Models;
using NuitsJp.GistGet.Business;

namespace NuitsJp.GistGet.Tests;

public class PackageYamlConverterTests
{
    [Fact]
    public void ToYaml_WithEmptyCollection_ShouldReturnEmptyPackagesYaml()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNullOrEmpty();
        yaml.ShouldContain("packages:");
        yaml.Trim().ShouldBe("packages: []");
    }

    [Fact]
    public void ToYaml_WithSinglePackage_ShouldSerializeCorrectly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();
        var package = new PackageDefinition("AkelPad.AkelPad", "4.9.8");
        collection.Add(package);

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNullOrEmpty();
        yaml.ShouldContain("packages:");
        yaml.ShouldContain("- id: AkelPad.AkelPad");
        yaml.ShouldContain("version: 4.9.8");
    }

    [Fact]
    public void ToYaml_WithMultiplePackages_ShouldSerializeInSortedOrder()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();
        collection.Add(new PackageDefinition("Zoom.Zoom"));
        collection.Add(new PackageDefinition("AkelPad.AkelPad"));
        collection.Add(new PackageDefinition("Microsoft.VisualStudioCode"));

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNullOrEmpty();
        var lines = yaml.Split('\n');
        var packageLines = lines.Where(line => line.Contains("- id:")).ToArray();
        packageLines.Length.ShouldBe(3);
        packageLines[0].ShouldContain("AkelPad.AkelPad");
        packageLines[1].ShouldContain("Microsoft.VisualStudioCode");
        packageLines[2].ShouldContain("Zoom.Zoom");
    }

    [Fact]
    public void ToYaml_WithFullPackageProperties_ShouldSerializeAllProperties()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();
        var package = new PackageDefinition(
            id: "AkelPad.AkelPad",
            version: "4.9.8",
            uninstall: "--silent",
            architecture: "x64",
            scope: "user",
            source: "winget",
            custom: "--force"
        );
        collection.Add(package);

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldContain("id: AkelPad.AkelPad");
        yaml.ShouldContain("version: 4.9.8");
        yaml.ShouldContain("uninstall: --silent");
        yaml.ShouldContain("architecture: x64");
        yaml.ShouldContain("scope: user");
        yaml.ShouldContain("source: winget");
        yaml.ShouldContain("custom: --force");
    }

    [Fact]
    public void FromYaml_WithValidYaml_ShouldDeserializeCorrectly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var yaml = """
            packages:
              - id: AkelPad.AkelPad
                version: 4.9.8
              - id: Microsoft.VisualStudioCode
            """;

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(2);

        var akelpad = collection.FindById("AkelPad.AkelPad");
        akelpad.ShouldNotBeNull();
        akelpad.Id.ShouldBe("AkelPad.AkelPad");
        akelpad.Version.ShouldBe("4.9.8");

        var vscode = collection.FindById("Microsoft.VisualStudioCode");
        vscode.ShouldNotBeNull();
        vscode.Id.ShouldBe("Microsoft.VisualStudioCode");
        vscode.Version.ShouldBeNull();
    }

    [Fact]
    public void FromYaml_WithEmptyPackages_ShouldReturnEmptyCollection()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var yaml = "packages: []";

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(0);
    }

    [Fact]
    public void FromYaml_WithInvalidYaml_ShouldThrowArgumentException()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var invalidYaml = "invalid: yaml: content";

        // Act & Assert
        Should.Throw<ArgumentException>(() => converter.FromYaml(invalidYaml));
    }

    [Fact]
    public void FromYaml_WithNullOrEmptyInput_ShouldThrowArgumentException()
    {
        // Arrange
        var converter = new PackageYamlConverter();

        // Act & Assert
        Should.Throw<ArgumentException>(() => converter.FromYaml(null!));
        Should.Throw<ArgumentException>(() => converter.FromYaml(string.Empty));
        Should.Throw<ArgumentException>(() => converter.FromYaml("   "));
    }

    [Fact]
    public void RoundTrip_ShouldPreservePackageData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();
        originalCollection.Add(new PackageDefinition("AkelPad.AkelPad", "4.9.8", "--silent", "x64", "user", "winget", "--force"));
        originalCollection.Add(new PackageDefinition("Microsoft.VisualStudioCode"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(originalCollection.Count);

        var originalAkelpad = originalCollection.FindById("AkelPad.AkelPad")!;
        var deserializedAkelpad = deserializedCollection.FindById("AkelPad.AkelPad")!;

        deserializedAkelpad.Id.ShouldBe(originalAkelpad.Id);
        deserializedAkelpad.Version.ShouldBe(originalAkelpad.Version);
        deserializedAkelpad.Uninstall.ShouldBe(originalAkelpad.Uninstall);
        deserializedAkelpad.Architecture.ShouldBe(originalAkelpad.Architecture);
        deserializedAkelpad.Scope.ShouldBe(originalAkelpad.Scope);
        deserializedAkelpad.Source.ShouldBe(originalAkelpad.Source);
        deserializedAkelpad.Custom.ShouldBe(originalAkelpad.Custom);
    }
}