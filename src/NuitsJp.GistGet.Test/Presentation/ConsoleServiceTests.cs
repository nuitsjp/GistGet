using System.Diagnostics;
using Moq;
using NuitsJp.GistGet.Infrastructure;
using NuitsJp.GistGet.Presentation;
using Shouldly;

namespace NuitsJp.GistGet.Test.Presentation;

[Collection("Console redirection")]
public class ConsoleServiceTests
{
    [Fact]
    public void WriteInfo_WritesMessageToConsole()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteInfo("hello");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.Out.ToString().ShouldContain("hello");
    }

    [Fact]
    public void WriteWarning_PrefixesMessageWithIndicator()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteWarning("be careful");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.Out.ToString().ShouldContain("! be careful");
    }

    [Fact]
    public void WriteSuccess_PrefixesMessageWithIndicator()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteSuccess("done");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.Out.ToString().ShouldContain("[OK] done");
    }

    [Fact]
    public void WriteSuccess_SetsForegroundColorToGreen()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteSuccess("done");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.ForegroundColorChanges.ShouldBe([ConsoleColor.Green, ConsoleColor.Gray]);
    }

    [Fact]
    public void ReadLine_ReturnsInputFromConsole()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy
        {
            In = new StringReader("user input\n")
        };
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        var result = target.ReadLine();

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        result.ShouldBe("user input");
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

        IConsoleService target = new ConsoleService(processRunner.Object, new FakeConsoleProxy());

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
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteStep(3, 10, "Installing package");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.Out.ToString().ShouldContain("[3/10] Installing package");
    }

    [Fact]
    public void WriteError_WritesErrorMessageToStandardError()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteError("Something went wrong");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.Error.ToString().ShouldContain("[ERR] Something went wrong");
    }

    [Fact]
    public void WriteError_SetsForegroundColorToRed()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy();
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        target.WriteError("Something went wrong");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.ForegroundColorChanges.ShouldBe([ConsoleColor.Red, ConsoleColor.Gray]);
    }

    [Fact]
    public void WriteProgress_WhenCursorVisibilityCannotBeSet_FallsBackToOneShotMessage()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy
        {
            ThrowOnCursorVisible = true
        };
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        var exception = Record.Exception(() =>
        {
            using var _ = target.WriteProgress("Loading...");
        });

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        exception.ShouldBeNull();
        console.Out.ToString().ShouldContain("... Loading...");
    }

    [Fact]
    public void WriteProgress_WhenOutputRedirected_DoesNotTrySpinner()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var console = new FakeConsoleProxy
        {
            IsOutputRedirected = true
        };
        var processRunner = new Mock<IProcessRunner>();
        IConsoleService target = new ConsoleService(processRunner.Object, console);

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        using var _ = target.WriteProgress("Fetching...");

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        console.CursorVisibleTouched.ShouldBeFalse();
        console.Out.ToString().ShouldContain("... Fetching...");
    }

    private sealed class FakeConsoleProxy : IConsoleProxy
    {
        public StringWriter Out { get; } = new();
        public StringWriter Error { get; } = new();
        public StringReader In { get; init; } = new(string.Empty);
        public bool IsOutputRedirected { get; init; }
        public bool IsErrorRedirected => false;
        public bool ThrowOnCursorVisible { get; init; }
        public bool CursorVisibleTouched { get; private set; }
        public int BufferWidth { get; } = 80;

        public bool CursorVisible
        {
            get;
            set
            {
                CursorVisibleTouched = true;
                if (ThrowOnCursorVisible)
                {
                    throw new IOException("Invalid handle");
                }

                field = value;
            }
        }

        public ConsoleColor ForegroundColor
        {
            get;
            set
            {
                ForegroundColorChanges.Add(value);
                field = value;
            }
        } = ConsoleColor.Gray;

        public void Write(string value) => Out.Write(value);

        public void WriteLine(string value) => Out.WriteLine(value);

        public void WriteErrorLine(string value) => Error.WriteLine(value);

        public string? ReadLine() => In.ReadLine();

        public List<ConsoleColor> ForegroundColorChanges { get; } = new();

    }
}




