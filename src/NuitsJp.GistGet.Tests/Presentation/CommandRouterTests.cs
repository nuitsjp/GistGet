using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Business.Models;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Os;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Models;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Presentation.GistConfig;
using NuitsJp.GistGet.Presentation.Login;
using NuitsJp.GistGet.Presentation.Sync;
using NuitsJp.GistGet.Presentation.WinGet;
using NuitsJp.GistGet.Presentation.File;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation;

public class CommandRouterTests
{
    private readonly Mock<ILogger<CommandRouter>> _mockLogger;
    private readonly Mock<IErrorMessageService> _mockErrorMessageService;
    private readonly Mock<IGitHubAuthService> _mockAuthService;
    private readonly Mock<IGistManager> _mockGistManager;

    // Command dependencies
    private readonly Mock<IGistConfigService> _mockGistConfigService;
    private readonly Mock<IGistConfigConsole> _mockGistConfigConsole;
    private readonly Mock<ILogger<GistSetCommand>> _mockGistSetLogger;
    private readonly Mock<ILogger<GistStatusCommand>> _mockGistStatusLogger;
    private readonly Mock<ILogger<GistShowCommand>> _mockGistShowLogger;
    private readonly Mock<ILogger<LoginCommand>> _mockLoginLogger;
    private readonly Mock<ILoginConsole> _mockLoginConsole;
    private readonly Mock<ILogger<LogoutCommand>> _mockLogoutLogger;
    private readonly Mock<ILogoutConsole> _mockLogoutConsole;
    private readonly Mock<IGistSyncService> _mockGistSyncService;
    private readonly Mock<ISyncConsole> _mockSyncConsole;
    private readonly Mock<ILogger<SyncCommand>> _mockSyncLogger;
    private readonly Mock<IPackageManagementService> _mockPackageManagementService;
    private readonly Mock<IWinGetClient> _mockWinGetClient;
    private readonly Mock<IOsService> _mockOsService;
    private readonly Mock<IWinGetConsole> _mockWinGetConsole;
    private readonly Mock<ILogger<WinGetCommand>> _mockWinGetLogger;
    private readonly Mock<IPackageYamlConverter> _mockPackageYamlConverter;

    // New command dependencies
    private readonly Mock<ILogger<GistClearCommand>> _mockGistClearLogger;
    private readonly Mock<IFileConsole> _mockFileConsole;
    private readonly Mock<ILogger<DownloadCommand>> _mockDownloadLogger;
    private readonly Mock<ILogger<UploadCommand>> _mockUploadLogger;
    private readonly Mock<IGitHubGistClient> _mockGitHubGistClient;

    private readonly CommandRouter _commandRouter;

    public CommandRouterTests()
    {
        // Router dependencies
        _mockLogger = new Mock<ILogger<CommandRouter>>();
        _mockErrorMessageService = new Mock<IErrorMessageService>();
        _mockAuthService = new Mock<IGitHubAuthService>();
        _mockGistManager = new Mock<IGistManager>();

        // Command dependencies
        _mockGistConfigService = new Mock<IGistConfigService>();
        _mockGistConfigConsole = new Mock<IGistConfigConsole>();
        _mockGistSetLogger = new Mock<ILogger<GistSetCommand>>();
        _mockGistStatusLogger = new Mock<ILogger<GistStatusCommand>>();
        _mockGistShowLogger = new Mock<ILogger<GistShowCommand>>();
        _mockLoginLogger = new Mock<ILogger<LoginCommand>>();
        _mockLoginConsole = new Mock<ILoginConsole>();
        _mockLogoutLogger = new Mock<ILogger<LogoutCommand>>();
        _mockLogoutConsole = new Mock<ILogoutConsole>();
        _mockGistSyncService = new Mock<IGistSyncService>();
        _mockSyncConsole = new Mock<ISyncConsole>();
        _mockSyncLogger = new Mock<ILogger<SyncCommand>>();
        _mockPackageManagementService = new Mock<IPackageManagementService>();
        _mockWinGetClient = new Mock<IWinGetClient>();
        _mockOsService = new Mock<IOsService>();
        _mockWinGetConsole = new Mock<IWinGetConsole>();
        _mockWinGetLogger = new Mock<ILogger<WinGetCommand>>();
        _mockPackageYamlConverter = new Mock<IPackageYamlConverter>();

        // New command mocks
        _mockGistClearLogger = new Mock<ILogger<GistClearCommand>>();
        _mockFileConsole = new Mock<IFileConsole>();
        _mockDownloadLogger = new Mock<ILogger<DownloadCommand>>();
        _mockUploadLogger = new Mock<ILogger<UploadCommand>>();
        _mockGitHubGistClient = new Mock<IGitHubGistClient>();

        // Setup default successful returns for all commands
        _mockGistConfigService.Setup(x => x.ConfigureGistAsync(It.IsAny<GistConfigRequest>()))
            .ReturnsAsync(new GistConfigResult { IsSuccess = true, GistId = "test", FileName = "test.yaml" });
        _mockGistSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(new SyncResult { ExitCode = 0 });
        _mockPackageManagementService.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);
        _mockPackageManagementService.Setup(x => x.UninstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);
        _mockPackageManagementService.Setup(x => x.UpgradePackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);
        _mockPackageManagementService.Setup(x => x.ExtractPackageId(It.IsAny<string[]>())).Returns("test-package");
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);
        _mockAuthService.Setup(x => x.LogoutAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);

        // Create actual command instances with mocked dependencies
        var gistSetCommand = new GistSetCommand(_mockGistConfigService.Object, _mockGistConfigConsole.Object, _mockGistSetLogger.Object);
        var gistStatusCommand = new GistStatusCommand(_mockAuthService.Object, _mockGistManager.Object, _mockGistConfigConsole.Object, _mockGistStatusLogger.Object);
        var gistShowCommand = new GistShowCommand(_mockAuthService.Object, _mockGistManager.Object, _mockPackageYamlConverter.Object, _mockGistConfigConsole.Object, _mockGistShowLogger.Object);
        var gistClearCommand = new GistClearCommand(_mockGistManager.Object, _mockGistConfigConsole.Object, _mockGistClearLogger.Object);
        var loginCommand = new LoginCommand(_mockAuthService.Object, _mockLoginConsole.Object, _mockLoginLogger.Object);
        var logoutCommand = new LogoutCommand(_mockAuthService.Object, _mockLogoutConsole.Object, _mockLogoutLogger.Object);
        var downloadCommand = new DownloadCommand(_mockGistManager.Object, _mockFileConsole.Object, _mockDownloadLogger.Object);
        var uploadCommand = new UploadCommand(_mockGistManager.Object, _mockGitHubGistClient.Object, _mockFileConsole.Object, _mockUploadLogger.Object);
        var syncCommand = new SyncCommand(_mockGistSyncService.Object, _mockSyncConsole.Object, _mockSyncLogger.Object);
        var winGetCommand = new WinGetCommand(_mockPackageManagementService.Object, _mockWinGetClient.Object, _mockOsService.Object, _mockWinGetConsole.Object, _mockWinGetLogger.Object);

        _commandRouter = new CommandRouter(
            gistSetCommand,
            gistStatusCommand,
            gistShowCommand,
            gistClearCommand,
            syncCommand,
            winGetCommand,
            _mockLogger.Object,
            _mockErrorMessageService.Object,
            _mockAuthService.Object,
            _mockGistManager.Object,
            loginCommand,
            logoutCommand,
            downloadCommand,
            uploadCommand);
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("install")]
    [InlineData("uninstall")]
    [InlineData("upgrade")]
    public async Task ExecuteAsync_WhenCommandRequiresAuthentication_ShouldCallEnsureAuthenticatedAsync(string command)
    {
        // Arrange
        var args = new[] { command, "test-package" };
        _mockAuthService.SetupSequence(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false)  // First call - not authenticated
            .ReturnsAsync(true);  // After login - authenticated
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.AtLeastOnce);
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
    }

    [Theory]
    [InlineData("gist", "set")]
    [InlineData("gist", "show")]
    public async Task ExecuteAsync_WhenGistSubCommandRequiresAuthentication_ShouldCallEnsureAuthenticatedAsync(string command, string subCommand)
    {
        // Arrange
        var args = new[] { command, subCommand };
        _mockAuthService.SetupSequence(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false)  // First call in CommandRouter - not authenticated
            .ReturnsAsync(true)   // After AuthenticateAsync in CommandRouter - authenticated
            .ReturnsAsync(true);  // Subsequent calls from commands - authenticated
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);

        // Additional mocks for specific commands
        if (subCommand == "set")
        {
            _mockGistConfigConsole.Setup(x => x.RequestGistConfiguration(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(("test-gist", "test.yaml"));
            _mockGistConfigService.Setup(x => x.ConfigureGistAsync(It.IsAny<GistConfigRequest>()))
                .ReturnsAsync(new GistConfigResult { IsSuccess = true, GistId = "test-gist", FileName = "test.yaml" });
        }
        else if (subCommand == "show")
        {
            _mockGistManager.Setup(x => x.GetConfigurationAsync()).ReturnsAsync(new GistConfiguration
            {
                GistId = "test-gist",
                FileName = "test.yaml",
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(new PackageCollection());
            _mockPackageYamlConverter.Setup(x => x.ToYaml(It.IsAny<PackageCollection>())).Returns("packages: []");
        }

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.AtLeastOnce);
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
    }

    [Theory]
    [InlineData("gist", "status")]
    [InlineData("search")]
    [InlineData("list")]
    public async Task ExecuteAsync_WhenCommandDoesNotRequireAuthentication_ShouldNotCallEnsureAuthenticatedAsync(string command, string subCommand = null)
    {
        // Arrange
        var args = subCommand != null ? new[] { command, subCommand } : new[] { command };

        // Set up per-test mocks for specific commands
        if (command == "gist" && subCommand == "status")
        {
            // GistStatusCommand for unauthenticated users should just show status
            _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(false);
            _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(false);
        }

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);

        // For CommandRouter's authentication flow, these should not be called
        // Note: Individual commands may call these, but CommandRouter should not
        // We verify CommandRouter didn't call authentication flow for these commands
        if (command == "gist" && subCommand == "status")
        {
            // GistStatusCommand calls IsAuthenticatedAsync directly, which is OK
            _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Never);
        }
        else
        {
            // For search/list, neither should be called
            _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Never);
            _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Never);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoginCommand_ShouldCallAuthenticateAsyncDirectly()
    {
        // Arrange
        var args = new[] { "login" };

        // Act  
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        // LoginCommand calls AuthenticateAsync directly, not through EnsureAuthenticatedAsync
        _mockAuthService.Verify(x => x.AuthenticateAsync(), Times.Once);
        // CommandRouter should not call IsAuthenticatedAsync for login command
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Never);
    }

    [Theory]
    [InlineData("add", "install")]
    [InlineData("remove", "uninstall")]
    [InlineData("rm", "uninstall")]
    [InlineData("update", "upgrade")]
    [InlineData("ls", "list")]
    [InlineData("find", "search")]
    [InlineData("view", "show")]
    [InlineData("config", "settings")]
    public async Task ExecuteAsync_CommandAliases_ShouldBeNormalizedToCanonicalCommand(string alias, string expectedCommand)
    {
        // Arrange
        var args = new[] { alias, "test-package" };

        // Mock for authenticated user (needed for COM commands)
        _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);

        // Verify the alias was normalized by checking the appropriate service was called
        switch (expectedCommand)
        {
            case "install":
            case "uninstall":
            case "upgrade":
                _mockPackageManagementService.Verify(x => x.ExtractPackageId(It.IsAny<string[]>()), Times.Once);
                break;
                // Passthrough commands don't need specific verification as they go to WinGetCommand
        }
    }
}