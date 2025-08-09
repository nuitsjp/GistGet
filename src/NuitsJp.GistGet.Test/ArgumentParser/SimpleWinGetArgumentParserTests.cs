using Xunit;
using FluentAssertions;
using NuitsJp.GistGet.ArgumentParser;

namespace NuitsJp.GistGet.Tests.ArgumentParser;

/// <summary>
/// Simple unit tests for WinGetArgumentParser
/// Tests basic functionality without complex dependencies
/// </summary>
public class SimpleWinGetArgumentParserTests
{
    private readonly WinGetArgumentParser _parser;

    public SimpleWinGetArgumentParserTests()
    {
        _parser = new WinGetArgumentParser();
    }

    [Fact]
    public void Should_Create_Parser_Successfully()
    {
        // Act & Assert
        _parser.Should().NotBeNull();
    }

    [Fact]
    public void Should_Build_Root_Command_Successfully()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        rootCommand.Should().NotBeNull();
        rootCommand.Name.Should().Be("gistget");
    }

    [Fact]
    public void Root_Command_Should_Have_Subcommands()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        rootCommand.Subcommands.Should().NotBeEmpty();
        
        var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
        commandNames.Should().Contain("install");
        commandNames.Should().Contain("list");
        commandNames.Should().Contain("upgrade");
        commandNames.Should().Contain("search");
    }

    [Fact]
    public void Root_Command_Should_Have_Global_Options()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        rootCommand.Options.Should().NotBeEmpty();
        
        var optionNames = rootCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("verbose-logs");
        optionNames.Should().Contain("info");
    }

    [Theory]
    [InlineData("install")]
    [InlineData("list")]
    [InlineData("upgrade")]
    [InlineData("uninstall")]
    [InlineData("search")]
    [InlineData("show")]
    [InlineData("export")]
    [InlineData("import")]
    public void Should_Have_All_Major_Commands(string commandName)
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
        commandNames.Should().Contain(commandName);
    }

    [Fact]
    public void Should_Have_Source_Command_With_Subcommands()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        var sourceCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "source");
        sourceCommand.Should().NotBeNull();
        sourceCommand.Subcommands.Should().NotBeEmpty();
        
        var sourceSubcommands = sourceCommand.Subcommands.Select(c => c.Name).ToList();
        sourceSubcommands.Should().Contain("add");
        sourceSubcommands.Should().Contain("list");
        sourceSubcommands.Should().Contain("remove");
    }

    [Fact]
    public void Should_Have_Settings_Command_With_Subcommands()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        var settingsCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "settings");
        settingsCommand.Should().NotBeNull();
        settingsCommand.Subcommands.Should().NotBeEmpty();
        
        var settingsSubcommands = settingsCommand.Subcommands.Select(c => c.Name).ToList();
        settingsSubcommands.Should().Contain("export");
        settingsSubcommands.Should().Contain("reset");
    }
}