using GistGet.Models;
using GistGet.Utils;
using System.Collections.Generic;
using Xunit;

namespace GistGet.Tests.Utils;

public class YamlHelperTests
{
    [Fact]
    public void Serialize_ShouldProduceCorrectYaml()
    {
        // Arrange
        var packages = new Dictionary<string, GistGetPackage>
        {
            { "Test.Package", new GistGetPackage { Id = "Test.Package", Version = "1.0.0" } },
            { "Uninstall.Package", new GistGetPackage { Id = "Uninstall.Package", Uninstall = true } }
        };

        // Act
        var yaml = YamlHelper.Serialize(packages);

        // Assert
        Assert.Contains("Test.Package:", yaml);
        Assert.Contains("version: 1.0.0", yaml);
        Assert.Contains("Uninstall.Package:", yaml);
        Assert.Contains("uninstall: true", yaml);
    }

    [Fact]
    public void Deserialize_ShouldParseYamlCorrectly()
    {
        // Arrange
        var yaml = @"
Test.Package:
  version: 1.0.0
Uninstall.Package:
  uninstall: true
";

        // Act
        var packages = YamlHelper.Deserialize(yaml);

        // Assert
        Assert.Equal(2, packages.Count);
        Assert.True(packages.ContainsKey("Test.Package"));
        Assert.Equal("1.0.0", packages["Test.Package"].Version);
        Assert.True(packages.ContainsKey("Uninstall.Package"));
        Assert.True(packages["Uninstall.Package"].Uninstall);
    }

    [Fact]
    public void Deserialize_ShouldHandleEmptyPackageEntry()
    {
        // Arrange
        var yaml = @"
Empty.Package:
";

        // Act
        var packages = YamlHelper.Deserialize(yaml);

        // Assert
        Assert.True(packages.ContainsKey("Empty.Package"));
        Assert.NotNull(packages["Empty.Package"]);
        Assert.Equal("Empty.Package", packages["Empty.Package"].Id);
    }

    [Fact]
    public void Serialize_ShouldOmitUninstall_WhenFalse()
    {
        // Arrange
        var packages = new Dictionary<string, GistGetPackage>
        {
            { "Normal.Package", new GistGetPackage { Id = "Normal.Package", Uninstall = false } }
        };

        // Act
        var yaml = YamlHelper.Serialize(packages);

        // Assert
        Assert.Contains("Normal.Package:", yaml);
        Assert.DoesNotContain("uninstall:", yaml);
    }
}
