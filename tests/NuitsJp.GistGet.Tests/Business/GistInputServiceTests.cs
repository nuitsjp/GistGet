using Shouldly;
using Xunit;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Models;
using Moq;
using System.IO;

namespace NuitsJp.GistGet.Tests.Business;

public class GistInputServiceTests
{
    [Fact]
    public void ValidateGistId_WithValidId_ShouldNotThrow()
    {
        // Arrange
        var service = new GistInputService();
        var validGistId = "d239aabb67e60650fbcb2b20a8342be1";

        // Act & Assert
        Should.NotThrow(() => service.ValidateGistId(validGistId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void ValidateGistId_WithInvalidId_ShouldThrowArgumentException(string invalidGistId)
    {
        // Arrange
        var service = new GistInputService();

        // Act & Assert
        Should.Throw<ArgumentException>(() => service.ValidateGistId(invalidGistId));
    }

    [Fact]
    public void ValidateFileName_WithValidName_ShouldNotThrow()
    {
        // Arrange
        var service = new GistInputService();
        var validFileName = "packages.yaml";

        // Act & Assert
        Should.NotThrow(() => service.ValidateFileName(validFileName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid<file>name")]
    [InlineData("file*with?invalid:chars")]
    [InlineData("con.txt")]
    [InlineData("prn.yaml")]
    public void ValidateFileName_WithInvalidName_ShouldThrowArgumentException(string invalidFileName)
    {
        // Arrange
        var service = new GistInputService();

        // Act & Assert
        Should.Throw<ArgumentException>(() => service.ValidateFileName(invalidFileName));
    }

    [Fact]
    public void ExtractGistIdFromUrl_WithValidGistUrl_ShouldReturnGistId()
    {
        // Arrange
        var service = new GistInputService();
        var gistUrl = "https://gist.github.com/username/d239aabb67e60650fbcb2b20a8342be1";

        // Act
        var gistId = service.ExtractGistIdFromUrl(gistUrl);

        // Assert
        gistId.ShouldBe("d239aabb67e60650fbcb2b20a8342be1");
    }

    [Theory]
    [InlineData("d239aabb67e60650fbcb2b20a8342be1")]
    [InlineData("https://gist.github.com/user/d239aabb67e60650fbcb2b20a8342be1#file-packages-yaml")]
    [InlineData("https://gist.github.com/user/d239aabb67e60650fbcb2b20a8342be1/revisions")]
    public void ExtractGistIdFromUrl_WithVariousFormats_ShouldReturnGistId(string input)
    {
        // Arrange
        var service = new GistInputService();

        // Act
        var gistId = service.ExtractGistIdFromUrl(input);

        // Assert
        gistId.ShouldBe("d239aabb67e60650fbcb2b20a8342be1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://github.com/user/repo")]
    [InlineData("https://gist.github.com/user/")]
    [InlineData("invalid-url")]
    public void ExtractGistIdFromUrl_WithInvalidInput_ShouldThrowArgumentException(string invalidInput)
    {
        // Arrange
        var service = new GistInputService();

        // Act & Assert
        Should.Throw<ArgumentException>(() => service.ExtractGistIdFromUrl(invalidInput));
    }

    [Fact]
    public void CreateConfiguration_WithValidInputs_ShouldReturnValidConfiguration()
    {
        // Arrange
        var service = new GistInputService();
        var gistId = "d239aabb67e60650fbcb2b20a8342be1";
        var fileName = "packages.yaml";

        // Act
        var config = service.CreateConfiguration(gistId, fileName);

        // Assert
        config.ShouldNotBeNull();
        config.GistId.ShouldBe(gistId);
        config.FileName.ShouldBe(fileName);
        config.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        config.LastAccessedAt.ShouldBe(config.CreatedAt);
    }

    [Fact]
    public void GetDefaultFileName_ShouldReturnPackagesYaml()
    {
        // Arrange
        var service = new GistInputService();

        // Act
        var defaultFileName = service.GetDefaultFileName();

        // Assert
        defaultFileName.ShouldBe("packages.yaml");
    }

    [Fact]
    public void DisplayGistCreationInstructions_ShouldReturnInstructionText()
    {
        // Arrange
        var service = new GistInputService();

        // Act
        var instructions = service.GetGistCreationInstructions();

        // Assert
        instructions.ShouldNotBeNullOrEmpty();
        instructions.ShouldContain("GitHub");
        instructions.ShouldContain("Gist");
        instructions.ShouldContain("https://gist.github.com");
    }

    [Fact]
    public void FormatGistUrl_WithValidGistId_ShouldReturnFormattedUrl()
    {
        // Arrange
        var service = new GistInputService();
        var gistId = "d239aabb67e60650fbcb2b20a8342be1";

        // Act
        var url = service.FormatGistUrl(gistId);

        // Assert
        url.ShouldBe($"https://gist.github.com/{gistId}");
    }
}