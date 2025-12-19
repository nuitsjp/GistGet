using System.Diagnostics;
using GistGet.Infrastructure;
using GistGet.Presentation;
using Moq;
using Shouldly;

namespace GistGet.Test.Presentation;

[Collection("AnsiConsole collection")]
public class ConsoleServiceTests
{
    [Fact]
    public void WriteInfo_WritesMessageToConsole()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            target.WriteInfo("hello");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            writer.ToString().ShouldContain("hello");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteWarning_PrefixesMessageWithIndicator()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            target.WriteWarning("be careful");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            writer.ToString().ShouldContain("! be careful");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ReadLine_ReturnsInputFromConsole()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var input = new StringReader("user input\n");
        var originalIn = Console.In;
        Console.SetIn(input);
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = target.ReadLine();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe("user input");
        }
        finally
        {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SetClipboard_UsesPowershellAndEscapesText()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var processRunner = new Mock<IProcessRunner>();
        ProcessStartInfo? captured = null;
        processRunner
            .Setup(x => x.RunAsync(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(info => captured = info)
            .ReturnsAsync(0);

        IConsoleService target = new ConsoleService(processRunner.Object);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.SetClipboard("te'st");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        captured.ShouldNotBeNull();
        captured!.FileName.ShouldBe("powershell");
        captured.Arguments.ShouldContain("te''st");
        captured.UseShellExecute.ShouldBeFalse();
        captured.CreateNoWindow.ShouldBeTrue();
        captured.RedirectStandardOutput.ShouldBeTrue();
        captured.RedirectStandardError.ShouldBeTrue();
    }

    [Fact]
    public void WriteStep_WritesFormattedProgressMessage()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            target.WriteStep(3, 10, "Installing package");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            writer.ToString().ShouldContain("[3/10] Installing package");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteError_WritesErrorMessageToStandardError()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var writer = new StringWriter();
        var originalError = Console.Error;
        Console.SetError(writer);
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object);

        try
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            target.WriteError("Something went wrong");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            writer.ToString().ShouldContain("✗ Something went wrong");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }
}
