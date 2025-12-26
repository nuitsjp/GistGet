// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MemberCanBePrivate.Global
using System.CommandLine;
using System.Globalization;
using GistGet.Presentation;
using Moq;
using Shouldly;
using Spectre.Console.Testing;

namespace GistGet.Test.Presentation;

[Collection("Console redirection")]
public class CommandBuilderTests : IDisposable
{
    protected readonly Mock<IGistGetService> GistGetServiceMock = new();
    protected readonly TestConsole TestConsole = new();

    protected CommandBuilder CreateTarget()
    {
        return new CommandBuilder(GistGetServiceMock.Object, TestConsole);
    }

    public void Dispose()
    {
        TestConsole.Dispose();
        GC.SuppressFinalize(this);
    }

    public class Build : CommandBuilderTests
    {
        [Fact]
        public void RegistersTopLevelCommands()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var root = target.Build();
            var names = root.Subcommands.Select(cmd => cmd.Name).ToList();
            var expected = new[]
            {
                "sync", "auth", "install", "uninstall", "upgrade", "pin",
                "list", "search", "show", "source", "settings", "features", "hash", "validate",
                "configure", "download", "repair", "dscv3", "mcp"
            };

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            foreach (var command in expected)
            {
                names.ShouldContain(command);
            }

            names.ShouldNotContain("export");
            names.ShouldNotContain("import");
        }

        [Fact]
        public async Task JapaneseCulture_ShowsLocalizedHelp()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            var originalOut = Console.Out;
            var originalError = Console.Error;
            var writer = new StringWriter();

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            Console.SetOut(writer);
            Console.SetError(writer);

            var target = CreateTarget();
            var root = target.Build();

            try
            {
                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                var exitCode = await root.InvokeAsync("--help");
                var output = writer.ToString();

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                exitCode.ShouldBe(0);
                output.ShouldContain("説明:");
                output.ShouldContain("GistGet - Windows パッケージ マネージャーのクラウド同期ツール");
                output.ShouldContain("Gist とパッケージを同期します");
                output.ShouldContain("ヘルプと使用法を表示します");
                output.ShouldContain("バージョン情報を表示します");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                writer.Dispose();
            }
        }
    }

    public class SyncCommand : CommandBuilderTests
    {
        [Fact]
        public async Task WithChanges_PrintsSummaryAndCallsService()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            var result = new SyncResult
            {
                Installed = { new GistGetPackage { Id = "Package.A" } },
                Uninstalled = { new GistGetPackage { Id = "Package.B" } },
                PinUpdated = { new GistGetPackage { Id = "Package.C", Pin = "1.2.3" } },
                PinRemoved = { new GistGetPackage { Id = "Package.D" } }
            };

            GistGetServiceMock
                .Setup(x => x.SyncAsync("https://example.com/gist", "local.yaml"))
                .ReturnsAsync(result);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("sync --url https://example.com/gist --file local.yaml");
            var output = TestConsole.Output;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.SyncAsync("https://example.com/gist", "local.yaml"), Times.Once);
            output.ShouldContain("Installed 1 package");
            output.ShouldContain("Uninstalled 1 package");
            output.ShouldContain("Updated pin for 1 package");
            output.ShouldContain("Removed pin for 1 package");
            output.ShouldContain("Sync completed successfully.");
        }

        [Fact]
        public async Task WithFailures_PrintsErrorsWithoutSuccessMessage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            var package = new GistGetPackage { Id = "Broken.Package", Name = "Broken Package" };
            var result = new SyncResult
            {
                Failed = { [package] = -1978335189 }
            };

            GistGetServiceMock
                .Setup(x => x.SyncAsync(null, null))
                .ReturnsAsync(result);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("sync");
            var output = TestConsole.Output;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            output.ShouldContain("Failed 1 package");
            output.ShouldContain("Broken Package [Broken.Package]: exit code -1978335189");
            output.ShouldNotContain("Errors:");
            output.ShouldNotContain("Sync completed successfully.");
        }

        [Fact]
        public async Task WithMarkupInErrors_PrintsRawMessage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            var package = new GistGetPackage { Id = "Tag.Package", Name = "[not-a-tag] Package" };
            var result = new SyncResult
            {
                Failed = { [package] = -1 }
            };

            GistGetServiceMock
                .Setup(x => x.SyncAsync(null, null))
                .ReturnsAsync(result);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("sync");
            var output = TestConsole.Output;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            output.ShouldContain("[not-a-tag] Package");
        }

        [Fact]
        public async Task AlreadyInSync_PrintsNoChangesMessage()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            var result = new SyncResult();

            GistGetServiceMock
                .Setup(x => x.SyncAsync(null, null))
                .ReturnsAsync(result);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("sync");
            var output = TestConsole.Output;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            output.ShouldContain("Already in sync. No changes needed.");
        }
    }

    public class AuthCommands : CommandBuilderTests
    {
        [Fact]
        public async Task LoginLogoutStatus_CallCorrespondingServiceMethods()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await root.InvokeAsync("auth login");
            await root.InvokeAsync("auth status");
            await root.InvokeAsync("auth logout");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            GistGetServiceMock.Verify(x => x.AuthLoginAsync(), Times.Once);
            GistGetServiceMock.Verify(x => x.AuthStatusAsync(), Times.Once);
            GistGetServiceMock.Verify(x => x.AuthLogout(), Times.Once);
        }
    }

    public class InstallCommand : CommandBuilderTests
    {
        [Fact]
        public async Task WithAllOptions_PassesMappedInstallOptions()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            GistGetServiceMock
                .Setup(x => x.InstallAndSaveAsync(It.IsAny<InstallOptions>()))
                .ReturnsAsync(5);

            var command = string.Join(" ", new[]
            {
                "install --id Test.Package",
                "--version 1.2.3",
                "--scope user",
                "--architecture x64",
                "--location \"C:/Apps\"",
                "--interactive",
                "--silent",
                "--log \"C:/logs/app.log\"",
                "--override \"--custom\"",
                "--force",
                "--skip-dependencies",
                "--header \"X-Test:1\"",
                "--installer-type msi",
                "--custom \"--xyz\"",
                "--locale ja-JP",
                "--accept-package-agreements",
                "--accept-source-agreements",
                "--ignore-security-hash"
            });

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync(command);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(5);
            GistGetServiceMock.Verify(x => x.InstallAndSaveAsync(It.Is<InstallOptions>(o =>
                o.Id == "Test.Package" &&
                o.Version == "1.2.3" &&
                o.Scope == "user" &&
                o.Architecture == "x64" &&
                o.Location == "C:/Apps" &&
                o.Interactive &&
                o.Silent &&
                o.Log == "C:/logs/app.log" &&
                o.Override == "--custom" &&
                o.Force &&
                o.SkipDependencies &&
                o.Header == "X-Test:1" &&
                o.InstallerType == "msi" &&
                o.Custom == "--xyz" &&
                o.Locale == "ja-JP" &&
                o.AcceptPackageAgreements &&
                o.AcceptSourceAgreements &&
                o.AllowHashMismatch
            )), Times.Once);
        }
    }

    public class UninstallCommand : CommandBuilderTests
    {
        [Fact]
        public async Task WithOptions_PassesMappedUninstallOptions()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            GistGetServiceMock
                .Setup(x => x.UninstallAndSaveAsync(It.IsAny<UninstallOptions>()))
                .ReturnsAsync(3);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("uninstall --id Test.Package --scope machine --interactive --silent --force");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(3);
            GistGetServiceMock.Verify(x => x.UninstallAndSaveAsync(It.Is<UninstallOptions>(o =>
                o.Id == "Test.Package" &&
                o.Scope == "machine" &&
                o.Interactive &&
                o.Silent &&
                o.Force
            )), Times.Once);
        }
    }

    public class UpgradeCommand : CommandBuilderTests
    {
        [Fact]
        public async Task WithId_PassesUpgradeOptions()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();
            GistGetServiceMock
                .Setup(x => x.UpgradeAndSaveAsync(It.IsAny<UpgradeOptions>()))
                .ReturnsAsync(7);

            var command = string.Join(" ", new[]
            {
                "upgrade --id Test.Package",
                "--version 2.0.0",
                "--scope machine",
                "--architecture arm64",
                "--location \"D:/Apps\"",
                "--interactive",
                "--silent",
                "--log \"D:/logs/app.log\"",
                "--override \"--ovr\"",
                "--force",
                "--skip-dependencies",
                "--header \"X-Upgrade:1\"",
                "--installer-type exe",
                "--custom \"--arg\"",
                "--locale en-US",
                "--accept-package-agreements",
                "--accept-source-agreements",
                "--ignore-security-hash"
            });

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync(command);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(7);
            GistGetServiceMock.Verify(x => x.UpgradeAndSaveAsync(It.Is<UpgradeOptions>(o =>
                o.Id == "Test.Package" &&
                o.Version == "2.0.0" &&
                o.Scope == "machine" &&
                o.Architecture == "arm64" &&
                o.Location == "D:/Apps" &&
                o.Interactive &&
                o.Silent &&
                o.Log == "D:/logs/app.log" &&
                o.Override == "--ovr" &&
                o.Force &&
                o.SkipDependencies &&
                o.Header == "X-Upgrade:1" &&
                o.InstallerType == "exe" &&
                o.Custom == "--arg" &&
                o.Locale == "en-US" &&
                o.AcceptPackageAgreements &&
                o.AcceptSourceAgreements &&
                o.AllowHashMismatch
            )), Times.Once);
        }

        [Fact]
        public async Task WithoutId_PassesThroughToWinget()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            GistGetServiceMock
                .Setup(x => x.RunPassthroughAsync("upgrade", It.IsAny<string[]>()))
                .ReturnsAsync(11);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("upgrade");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(11);
            GistGetServiceMock.Verify(x => x.RunPassthroughAsync("upgrade", It.Is<string[]>(args =>
                args.Length == 0
            )), Times.Once);
            GistGetServiceMock.Verify(x => x.UpgradeAndSaveAsync(It.IsAny<UpgradeOptions>()), Times.Never);
        }

        [Fact]
        public async Task WithAll_PassesThroughToWinget()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            GistGetServiceMock
                .Setup(x => x.RunPassthroughAsync("upgrade", It.IsAny<string[]>()))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("upgrade --all");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.RunPassthroughAsync("upgrade", It.Is<string[]>(args =>
                args.Length == 1 && args[0] == "--all"
            )), Times.Once);
            GistGetServiceMock.Verify(x => x.UpgradeAndSaveAsync(It.IsAny<UpgradeOptions>()), Times.Never);
        }

        [Fact]
        public async Task WithAllAndOtherOptions_PassesThroughToWinget()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            GistGetServiceMock
                .Setup(x => x.RunPassthroughAsync("upgrade", It.IsAny<string[]>()))
                .ReturnsAsync(0);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("upgrade --all --silent --accept-package-agreements");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.RunPassthroughAsync("upgrade", It.Is<string[]>(args =>
                args.Length == 3 &&
                args[0] == "--all" &&
                args[1] == "--silent" &&
                args[2] == "--accept-package-agreements"
            )), Times.Once);
            GistGetServiceMock.Verify(x => x.UpgradeAndSaveAsync(It.IsAny<UpgradeOptions>()), Times.Never);
        }
    }

    public class PinCommand : CommandBuilderTests
    {
        [Fact]
        public async Task Add_WithGatingPinType_PassesPinType()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("pin add Test.Package --version 1.2.3 --gating --force");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.PinAddAndSaveAsync("Test.Package", "1.2.3", "gating", true), Times.Once);
        }

        [Fact]
        public async Task Add_WithoutPinType_UsesNull()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("pin add Sample.Package --version 9.9.9");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.PinAddAndSaveAsync("Sample.Package", "9.9.9", null, false), Times.Once);
        }

        [Fact]
        public async Task Remove_CallsPinRemove()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("pin remove Old.Package");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.PinRemoveAndSaveAsync("Old.Package"), Times.Once);
        }

        [Fact]
        public async Task List_ForwardsArgumentsToWinget()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("pin list --source msstore");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.RunPassthroughAsync("pin", It.Is<string[]>(args =>
                args.SequenceEqual(new[] { "list", "--source", "msstore" })
            )), Times.Once);
        }

        [Fact]
        public async Task Reset_ForwardsArgumentsToWinget()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("pin reset --force");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.RunPassthroughAsync("pin", It.Is<string[]>(args =>
                args.SequenceEqual(new[] { "reset", "--force" })
            )), Times.Once);
        }
    }

    public class WingetPassthroughCommands : CommandBuilderTests
    {
        [Fact]
        public async Task List_ForwardsToRunner()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = CreateTarget();
            var root = target.Build();

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exitCode = await root.InvokeAsync("list --source msstore");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exitCode.ShouldBe(0);
            GistGetServiceMock.Verify(x => x.RunPassthroughAsync("list", It.Is<string[]>(args =>
                args.SequenceEqual(new[] { "--source", "msstore" })
            )), Times.Once);
        }
    }
}
