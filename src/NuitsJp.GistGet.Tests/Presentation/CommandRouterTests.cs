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
    private readonly Mock<IGistSyncService> _mockGistSyncService;
    private readonly Mock<ISyncConsole> _mockSyncConsole;
    private readonly Mock<ILogger<SyncCommand>> _mockSyncLogger;
    private readonly Mock<IPackageManagementService> _mockPackageManagementService;
    private readonly Mock<IWinGetClient> _mockWinGetClient;
    private readonly Mock<IOsService> _mockOsService;
    private readonly Mock<IWinGetConsole> _mockWinGetConsole;
    private readonly Mock<ILogger<WinGetCommand>> _mockWinGetLogger;
    private readonly Mock<IPackageYamlConverter> _mockPackageYamlConverter;

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
        _mockGistSyncService = new Mock<IGistSyncService>();
        _mockSyncConsole = new Mock<ISyncConsole>();
        _mockSyncLogger = new Mock<ILogger<SyncCommand>>();
        _mockPackageManagementService = new Mock<IPackageManagementService>();
        _mockWinGetClient = new Mock<IWinGetClient>();
        _mockOsService = new Mock<IOsService>();
        _mockWinGetConsole = new Mock<IWinGetConsole>();
        _mockWinGetLogger = new Mock<ILogger<WinGetCommand>>();
        _mockPackageYamlConverter = new Mock<IPackageYamlConverter>();

        // Setup default successful returns for all commands
        _mockGistConfigService.Setup(x => x.ConfigureGistAsync(It.IsAny<GistConfigRequest>()))
            .ReturnsAsync(new GistConfigResult { IsSuccess = true, GistId = "test", FileName = "test.yaml" });
        _mockGistSyncService.Setup(x => x.SyncAsync()).ReturnsAsync(new SyncResult { ExitCode = 0 });
        _mockPackageManagementService.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);
        _mockPackageManagementService.Setup(x => x.UninstallPackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);
        _mockPackageManagementService.Setup(x => x.UpgradePackageAsync(It.IsAny<string[]>())).ReturnsAsync(0);
        _mockPackageManagementService.Setup(x => x.ExtractPackageId(It.IsAny<string[]>())).Returns("test-package");
        _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(true);
        _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);

        // Create actual command instances with mocked dependencies
        var gistSetCommand = new GistSetCommand(_mockGistConfigService.Object, _mockGistConfigConsole.Object, _mockGistSetLogger.Object);
        var gistStatusCommand = new GistStatusCommand(_mockAuthService.Object, _mockGistManager.Object, _mockGistConfigConsole.Object, _mockGistStatusLogger.Object);
        var gistShowCommand = new GistShowCommand(_mockAuthService.Object, _mockGistManager.Object, _mockPackageYamlConverter.Object, _mockGistConfigConsole.Object, _mockGistShowLogger.Object);
        var loginCommand = new LoginCommand(_mockAuthService.Object, _mockLoginConsole.Object, _mockLoginLogger.Object);
        var syncCommand = new SyncCommand(_mockGistSyncService.Object, _mockSyncConsole.Object, _mockSyncLogger.Object);
        var winGetCommand = new WinGetCommand(_mockPackageManagementService.Object, _mockWinGetClient.Object, _mockOsService.Object, _mockWinGetConsole.Object, _mockWinGetLogger.Object);

        _commandRouter = new CommandRouter(
            gistSetCommand,
            gistStatusCommand,
            gistShowCommand,
            syncCommand,
            winGetCommand,
            _mockLogger.Object,
            _mockErrorMessageService.Object,
            _mockAuthService.Object,
            _mockGistManager.Object,
            loginCommand);
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
            // GistStatusCommand needs these additional setups
            _mockAuthService.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);
            _mockGistManager.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
            _mockGistManager.Setup(x => x.GetConfigurationAsync()).ReturnsAsync(new GistConfiguration 
            { 
                GistId = "test-gist", 
                FileName = "test.yaml",
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            });
            _mockGistManager.Setup(x => x.ValidateGistAccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockGistManager.Setup(x => x.GetGistPackagesAsync()).ReturnsAsync(new PackageCollection());
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
}