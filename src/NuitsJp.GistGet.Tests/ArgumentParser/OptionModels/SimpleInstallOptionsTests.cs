using Xunit;
using FluentAssertions;
using NuitsJp.GistGet.ArgumentParser.OptionModels;

namespace NuitsJp.GistGet.Tests.ArgumentParser.OptionModels;

/// <summary>
/// Simple unit tests for InstallOptions validation
/// Tests basic validation logic
/// </summary>
public class SimpleInstallOptionsTests
{
    [Fact]
    public void Should_Create_InstallOptions_Successfully()
    {
        // Act
        var options = new InstallOptions();

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void Should_Set_Properties_Correctly()
    {
        // Act
        var options = new InstallOptions
        {
            Id = "Git.Git",
            Silent = true,
            Force = false
        };

        // Assert
        options.Id.Should().Be("Git.Git");
        options.Silent.Should().BeTrue();
        options.Force.Should().BeFalse();
    }

    [Fact]
    public void Should_Have_ValidateOptions_Method()
    {
        // Arrange
        var options = new InstallOptions { Id = "Git.Git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<List<string>>();
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Id()
    {
        // Arrange
        var options = new InstallOptions { Id = "Git.Git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Query()
    {
        // Arrange
        var options = new InstallOptions { Query = "git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Name()
    {
        // Arrange
        var options = new InstallOptions { Name = "Git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.Should().BeEmpty();
    }
}