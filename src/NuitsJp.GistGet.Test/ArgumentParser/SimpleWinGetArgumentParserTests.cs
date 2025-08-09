using NuitsJp.GistGet.ArgumentParser;
using Shouldly;

namespace NuitsJp.GistGet.Test.ArgumentParser;

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
        _parser.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Build_Root_Command_Successfully()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        rootCommand.ShouldNotBeNull();
        rootCommand!.Name.ShouldBe("gistget");
    }

    [Fact]
    public void Root_Command_Should_Have_Subcommands()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        rootCommand.Subcommands.ShouldNotBeEmpty();

        var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
        commandNames.ShouldContain("install");
        commandNames.ShouldContain("list");
        commandNames.ShouldContain("upgrade");
        commandNames.ShouldContain("search");
    }

    [Fact]
    public void Root_Command_Should_Have_Global_Options()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        rootCommand.Options.ShouldNotBeEmpty();

        var optionNames = rootCommand.Options.Select(o => o.Name).ToList();
        optionNames.ShouldContain("verbose-logs");
        optionNames.ShouldContain("info");
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
        commandNames.ShouldContain(commandName);
    }

    [Fact]
    public void Should_Have_Source_Command_With_Subcommands()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        var sourceCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "source");
        sourceCommand.ShouldNotBeNull();
        sourceCommand!.Subcommands.ShouldNotBeEmpty();

        var sourceSubcommands = sourceCommand.Subcommands.Select(c => c.Name).ToList();
        sourceSubcommands.ShouldContain("add");
        sourceSubcommands.ShouldContain("list");
        sourceSubcommands.ShouldContain("remove");
    }

    [Fact]
    public void Should_Have_Settings_Command_With_Subcommands()
    {
        // Act
        var rootCommand = _parser.BuildRootCommand();

        // Assert
        var settingsCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "settings");
        settingsCommand.ShouldNotBeNull();
        settingsCommand!.Subcommands.ShouldNotBeEmpty();

        var settingsSubcommands = settingsCommand.Subcommands.Select(c => c.Name).ToList();
        settingsSubcommands.ShouldContain("export");
        settingsSubcommands.ShouldContain("reset");
    }
}