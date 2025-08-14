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

    [Fact]
    public void RoundTrip_EmptyCollection_ShouldPreserveEmptyState()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        originalCollection.Count.ShouldBe(0);
        deserializedCollection.Count.ShouldBe(0);
        yaml.Trim().ShouldBe("packages: []");
    }

    [Fact]
    public void RoundTrip_SinglePackageMinimalFields_ShouldPreserveIdOnly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();
        originalCollection.Add(new PackageDefinition("Microsoft.PowerToys"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(1);
        var package = deserializedCollection.FindById("Microsoft.PowerToys")!;
        package.Id.ShouldBe("Microsoft.PowerToys");
        package.Version.ShouldBeNull();
        package.Uninstall.ShouldBeNull();
        package.Architecture.ShouldBeNull();
        package.Scope.ShouldBeNull();
        package.Source.ShouldBeNull();
        package.Custom.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_MultiplePackagesVariedFields_ShouldPreserveAllData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Package with minimal fields
        originalCollection.Add(new PackageDefinition("A.MinimalPackage"));

        // Package with some fields
        originalCollection.Add(new PackageDefinition("B.PartialPackage", version: "1.0.0", architecture: "x64"));

        // Package with all fields
        originalCollection.Add(new PackageDefinition("C.FullPackage", "2.0.0", "--silent", "x86", "machine", "msstore", "--quiet"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(3);

        // Verify minimal package
        var minimal = deserializedCollection.FindById("A.MinimalPackage")!;
        minimal.Id.ShouldBe("A.MinimalPackage");
        minimal.Version.ShouldBeNull();
        minimal.Uninstall.ShouldBeNull();
        minimal.Architecture.ShouldBeNull();
        minimal.Scope.ShouldBeNull();
        minimal.Source.ShouldBeNull();
        minimal.Custom.ShouldBeNull();

        // Verify partial package
        var partial = deserializedCollection.FindById("B.PartialPackage")!;
        partial.Id.ShouldBe("B.PartialPackage");
        partial.Version.ShouldBe("1.0.0");
        partial.Architecture.ShouldBe("x64");
        partial.Uninstall.ShouldBeNull();
        partial.Scope.ShouldBeNull();
        partial.Source.ShouldBeNull();
        partial.Custom.ShouldBeNull();

        // Verify full package
        var full = deserializedCollection.FindById("C.FullPackage")!;
        full.Id.ShouldBe("C.FullPackage");
        full.Version.ShouldBe("2.0.0");
        full.Uninstall.ShouldBe("--silent");
        full.Architecture.ShouldBe("x86");
        full.Scope.ShouldBe("machine");
        full.Source.ShouldBe("msstore");
        full.Custom.ShouldBe("--quiet");
    }

    [Fact]
    public void RoundTrip_WithOptionalFieldsCombinations_ShouldPreserveExactData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Various combinations of optional fields
        originalCollection.Add(new PackageDefinition("Test.VersionOnly", version: "1.0"));
        originalCollection.Add(new PackageDefinition("Test.UninstallOnly", uninstall: "--force"));
        originalCollection.Add(new PackageDefinition("Test.ArchOnly", architecture: "arm64"));
        originalCollection.Add(new PackageDefinition("Test.ScopeOnly", scope: "user"));
        originalCollection.Add(new PackageDefinition("Test.SourceOnly", source: "winget"));
        originalCollection.Add(new PackageDefinition("Test.CustomOnly", custom: "--override"));
        originalCollection.Add(new PackageDefinition("Test.VersionAndScope", version: "2.0", scope: "machine"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(7);

        var versionOnly = deserializedCollection.FindById("Test.VersionOnly")!;
        versionOnly.Version.ShouldBe("1.0");
        versionOnly.Uninstall.ShouldBeNull();

        var uninstallOnly = deserializedCollection.FindById("Test.UninstallOnly")!;
        uninstallOnly.Uninstall.ShouldBe("--force");
        uninstallOnly.Version.ShouldBeNull();

        var archOnly = deserializedCollection.FindById("Test.ArchOnly")!;
        archOnly.Architecture.ShouldBe("arm64");
        archOnly.Version.ShouldBeNull();

        var scopeOnly = deserializedCollection.FindById("Test.ScopeOnly")!;
        scopeOnly.Scope.ShouldBe("user");
        scopeOnly.Version.ShouldBeNull();

        var sourceOnly = deserializedCollection.FindById("Test.SourceOnly")!;
        sourceOnly.Source.ShouldBe("winget");
        sourceOnly.Version.ShouldBeNull();

        var customOnly = deserializedCollection.FindById("Test.CustomOnly")!;
        customOnly.Custom.ShouldBe("--override");
        customOnly.Version.ShouldBeNull();

        var versionAndScope = deserializedCollection.FindById("Test.VersionAndScope")!;
        versionAndScope.Version.ShouldBe("2.0");
        versionAndScope.Scope.ShouldBe("machine");
        versionAndScope.Uninstall.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_WithEmptyStringFields_ShouldTreatAsNull()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Package with empty string fields (should be treated as null)
        originalCollection.Add(new PackageDefinition("Test.EmptyFields", "", "", "", "", "", ""));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(1);
        var package = deserializedCollection.FindById("Test.EmptyFields")!;

        // Empty strings should be serialized as null and deserialized as null
        package.Version.ShouldBeNull();
        package.Uninstall.ShouldBeNull();
        package.Architecture.ShouldBeNull();
        package.Scope.ShouldBeNull();
        package.Source.ShouldBeNull();
        package.Custom.ShouldBeNull();

        // YAML should not contain empty string fields (OmitNull configuration)
        yaml.ShouldNotContain("version:");
        yaml.ShouldNotContain("uninstall:");
        yaml.ShouldNotContain("architecture:");
        yaml.ShouldNotContain("scope:");
        yaml.ShouldNotContain("source:");
        yaml.ShouldNotContain("custom:");
    }
}