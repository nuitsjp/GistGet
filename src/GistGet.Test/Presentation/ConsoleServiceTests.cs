using System.Diagnostics;
using GistGet.Infrastructure;
using GistGet.Presentation;
using Moq;
using Shouldly;

namespace GistGet.Test.Presentation;

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
        console.Error.ToString().ShouldContain("? Something went wrong");
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
        console.Out.ToString().ShouldContain("? Loading...");
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
        console.Out.ToString().ShouldContain("? Fetching...");
    }

    private sealed class FakeConsoleProxy : IConsoleProxy
    {
        private bool _cursorVisible;

        public StringWriter Out { get; } = new();
        public StringWriter Error { get; } = new();
        public StringReader In { get; set; } = new(String.Empty);
        public bool IsOutputRedirected { get; set; }
        public bool IsErrorRedirected { get; set; }
        public bool ThrowOnCursorVisible { get; set; }
        public bool CursorVisibleTouched { get; private set; }
        public int BufferWidth { get; set; } = 80;

        public bool CursorVisible
        {
            get => _cursorVisible;
            set
            {
                CursorVisibleTouched = true;
                if (ThrowOnCursorVisible)
                {
                    throw new IOException("Invalid handle");
                }

                _cursorVisible = value;
            }
        }

        public void Write(string value) => Out.Write(value);

        public void WriteLine(string value) => Out.WriteLine(value);

        public void WriteErrorLine(string value) => Error.WriteLine(value);

        public string? ReadLine() => In.ReadLine();
    }
}
