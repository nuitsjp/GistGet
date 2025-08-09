using NuitsJp.GistGet.ArgumentParser.OptionModels;
using Shouldly;

namespace NuitsJp.GistGet.Test.ArgumentParser.OptionModels;

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
        options.ShouldNotBeNull();
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
        options.Id.ShouldBe("Git.Git");
        options.Silent.ShouldBeTrue();
        options.Force.ShouldBeFalse();
    }

    [Fact]
    public void Should_Have_ValidateOptions_Method()
    {
        // Arrange
        var options = new InstallOptions { Id = "Git.Git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<List<string>>();
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Id()
    {
        // Arrange
        var options = new InstallOptions { Id = "Git.Git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Query()
    {
        // Arrange
        var options = new InstallOptions { Query = "git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Pass_Validation_With_Valid_Name()
    {
        // Arrange
        var options = new InstallOptions { Name = "Git" };

        // Act
        var result = options.ValidateOptions();

        // Assert
        result.ShouldBeEmpty();
    }
}