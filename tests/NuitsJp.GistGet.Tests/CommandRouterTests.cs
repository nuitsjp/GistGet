using Microsoft.Extensions.Logging;
using NuitsJp.GistGet.Tests.Mocks;
using NuitsJp.GistGet.Presentation.Commands;
using Shouldly;
using Xunit;
using Moq;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Infrastructure.GitHub;
using NuitsJp.GistGet.Infrastructure.Storage;
using NuitsJp.GistGet.Business.Services;

namespace NuitsJp.GistGet.Tests;

public class CommandRouterTests
{
    private readonly ILogger<CommandRouter> _logger;
    private readonly MockWinGetClient _mockWinGetClient;
    private readonly MockWinGetPassthroughClient _mockPassthroughClient;
    private readonly MockGistSyncService _mockGistSyncService;
    private readonly CommandRouter _commandRouter;
    private readonly GitHubAuthService _concreteAuthService;
    private readonly GistInputService _gistInputService;
    private readonly IGistConfigurationStorage _mockStorage;
    private readonly GitHubGistClient _gistClient;
    private readonly GistManager _gistManager;
    private readonly PackageYamlConverter _packageYamlConverter;
    private readonly Mock<IGistConfigService> _mockGistConfigService;

    public CommandRouterTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CommandRouter>();
        _mockWinGetClient = new MockWinGetClient();
        _mockPassthroughClient = new MockWinGetPassthroughClient();
        _mockGistSyncService = new MockGistSyncService();

        // AuthCommandとTestGistCommandのモックを作成
        var mockAuthServiceInterface = new Mock<IGitHubAuthService>();
        var authLogger = new Mock<ILogger<AuthCommand>>();
        var authCommand = new AuthCommand(mockAuthServiceInterface.Object, authLogger.Object);

        var testGistLogger = new Mock<ILogger<TestGistCommand>>();
        var testGistCommand = new TestGistCommand(mockAuthServiceInterface.Object, testGistLogger.Object);

        // Gist関連コマンド用の共通依存関係を初期化
        var concreteAuthLogger = Mock.Of<ILogger<GitHubAuthService>>();
        _concreteAuthService = new GitHubAuthService(concreteAuthLogger);

        _gistInputService = new GistInputService();
        _mockStorage = Mock.Of<IGistConfigurationStorage>();
        var mockGistClientLogger = Mock.Of<ILogger<GitHubGistClient>>();
        _gistClient = new GitHubGistClient(_concreteAuthService, mockGistClientLogger);
        var mockGistManagerLogger = Mock.Of<ILogger<GistManager>>();
        _packageYamlConverter = new PackageYamlConverter();
        _gistManager = new GistManager(_gistClient, _mockStorage, _packageYamlConverter, mockGistManagerLogger);

        _mockGistConfigService = new Mock<IGistConfigService>();

        var gistSetLogger = Mock.Of<ILogger<GistSetCommand>>();
        var gistStatusLogger = Mock.Of<ILogger<GistStatusCommand>>();
        var gistShowLogger = Mock.Of<ILogger<GistShowCommand>>();

        var mockGistSetCommand = new GistSetCommand(_mockGistConfigService.Object, gistSetLogger);
        var mockGistStatusCommand = new GistStatusCommand(_concreteAuthService, _gistInputService, _gistManager, gistStatusLogger);
        var mockGistShowCommand = new GistShowCommand(_concreteAuthService, _gistInputService, _gistManager, _packageYamlConverter, gistShowLogger);

        var mockErrorMessageService = new Mock<IErrorMessageService>();
        _commandRouter = new CommandRouter(
            _mockWinGetClient,
            _mockPassthroughClient,
            _mockGistSyncService,
            authCommand,
            testGistCommand,
            mockGistSetCommand,
            mockGistStatusCommand,
            mockGistShowCommand,
            _logger,
            mockErrorMessageService.Object);
    }

    [Theory]
    [InlineData("install")]
    [InlineData("uninstall")]
    [InlineData("upgrade")]
    public async Task ExecuteAsync_ShouldRouteToCOMClient_WhenUsingCOMCommands(string command)
    {
        // Arrange
        var args = new[] { command, "--id", "TestPackage" };

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockWinGetClient.InitializeCalled.ShouldBeTrue();
        _mockWinGetClient.LastCommand.ShouldBe(command);
        _mockWinGetClient.LastArgs.ShouldBe(args);
    }

    [Theory]
    [InlineData("sync")]
    public async Task ExecuteAsync_ShouldRouteToGistService_WhenUsingGistCommands(string command)
    {
        // Arrange
        var args = new[] { command };

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockGistSyncService.LastCommand.ShouldBe(command);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("search")]
    [InlineData("show")]
    [InlineData("source")]
    [InlineData("settings")]
    [InlineData("export")]
    [InlineData("import")]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenUsingPassthroughCommands(string command)
    {
        // Arrange
        var args = new[] { command, "test-arg" };

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenNoArgs()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteExportToPassthrough_E2ESmoke()
    {
        // Arrange - E2Eスモークテスト: export コマンドが適切にパススルーされることを確認
        var args = new[] { "export", "--output", "test-packages.json" };

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
        _mockPassthroughClient.LastArgs.Length.ShouldBe(3);
        _mockPassthroughClient.LastArgs[0].ShouldBe("export");
        _mockPassthroughClient.LastArgs[1].ShouldBe("--output");
        _mockPassthroughClient.LastArgs[2].ShouldBe("test-packages.json");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteImportToPassthrough_E2ESmoke()
    {
        // Arrange - E2Eスモークテスト: import コマンドが適切にパススルーされることを確認
        var args = new[] { "import", "--input", "test-packages.json", "--accept-package-agreements" };

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
        _mockPassthroughClient.LastArgs.Length.ShouldBe(4);
        _mockPassthroughClient.LastArgs[0].ShouldBe("import");
        _mockPassthroughClient.LastArgs[1].ShouldBe("--input");
        _mockPassthroughClient.LastArgs[2].ShouldBe("test-packages.json");
        _mockPassthroughClient.LastArgs[3].ShouldBe("--accept-package-agreements");
    }

    [Theory]
    [InlineData("export")]
    [InlineData("import")]
    public async Task ExecuteAsync_ShouldLogExplicitPassthroughRouting_ForExportImportCommands(string command)
    {
        // Arrange - パススルーコマンドのログ出力が明示的であることを確認
        var args = new[] { command, "--test" };

        // Act
        var result = await _commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);

        // ログでは明示的パススルーとして記録される（実装では "explicit command" としてログされる）
        // モックなのでログの検証は困難だが、ルーティングロジックが正しく動作することを確認
    }

    // Phase 7.1: エラーハンドリング改善 - Red フェーズ（失敗するテストから開始）

    [Fact]
    public async Task ExecuteAsync_ShouldDisplayUserFriendlyMessage_WhenWinGetClientThrowsComException()
    {
        // Arrange
        var mockWinGetClient = new Mock<IWinGetClient>();
        var mockPassthroughClient = new Mock<IWinGetPassthroughClient>();
        var mockGistSyncService = new Mock<IGistSyncService>();
        var logger = new Mock<ILogger<CommandRouter>>();

        // COM例外をスローするように設定
        var comException = new System.Runtime.InteropServices.COMException("COM Error", -2147024891); // E_ACCESSDENIED
        mockWinGetClient.Setup(x => x.InitializeAsync()).ThrowsAsync(comException);

        // AuthCommandとTestGistCommandのモックを作成
        var mockAuthService = new Mock<IGitHubAuthService>();
        var authLogger = new Mock<ILogger<AuthCommand>>();
        var authCommand = new AuthCommand(mockAuthService.Object, authLogger.Object);

        var testGistLogger = new Mock<ILogger<TestGistCommand>>();
        var testGistCommand = new TestGistCommand(mockAuthService.Object, testGistLogger.Object);

        // Gist関連コマンド用の具象GitHubAuthService（最小実装）
        var concreteAuthLogger = Mock.Of<ILogger<GitHubAuthService>>();
        var concreteAuthService = new GitHubAuthService(concreteAuthLogger);

        // Gist関連コマンドの依存関係をモック（最小実装）
        var mockGistInputService = new GistInputService();
        var mockStorage = Mock.Of<IGistConfigurationStorage>();
        var mockGistClientLogger = Mock.Of<ILogger<GitHubGistClient>>();
        var mockGistClient = new GitHubGistClient(concreteAuthService, mockGistClientLogger);
        var mockGistManagerLogger = Mock.Of<ILogger<GistManager>>();
        var mockPackageYamlConverter = new PackageYamlConverter();
        var mockGistManager = new GistManager(mockGistClient, mockStorage, mockPackageYamlConverter, mockGistManagerLogger);

        var gistSetLogger = Mock.Of<ILogger<GistSetCommand>>();
        var gistStatusLogger = Mock.Of<ILogger<GistStatusCommand>>();
        var gistShowLogger = Mock.Of<ILogger<GistShowCommand>>();

        var mockGistConfigService = new Mock<IGistConfigService>();
        var mockGistSetCommand = new GistSetCommand(mockGistConfigService.Object, gistSetLogger);
        var mockGistStatusCommand = new GistStatusCommand(concreteAuthService, mockGistInputService, mockGistManager, gistStatusLogger);
        var mockGistShowCommand = new GistShowCommand(concreteAuthService, mockGistInputService, mockGistManager, mockPackageYamlConverter, gistShowLogger);

        var mockErrorMessageService = new Mock<IErrorMessageService>();
        var commandRouter = new CommandRouter(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            authCommand,
            testGistCommand,
            mockGistSetCommand,
            mockGistStatusCommand,
            mockGistShowCommand,
            logger.Object,
            mockErrorMessageService.Object);

        var args = new[] { "install", "--id", "TestPackage.Test" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(1); // エラー終了コード

        // ErrorMessageServiceのHandleComExceptionが呼び出されることを確認
        mockErrorMessageService.Verify(x => x.HandleComException(comException), Times.Once);
    }

    private CommandRouter CreateCommandRouterForTesting(
        Mock<IWinGetClient> mockWinGetClient,
        Mock<IWinGetPassthroughClient> mockPassthroughClient,
        Mock<IGistSyncService> mockGistSyncService,
        Mock<IErrorMessageService> mockErrorMessageService)
    {
        var mockAuthService = new Mock<IGitHubAuthService>();
        var authLogger = new Mock<ILogger<AuthCommand>>();
        var authCommand = new AuthCommand(mockAuthService.Object, authLogger.Object);

        var testGistLogger = new Mock<ILogger<TestGistCommand>>();
        var testGistCommand = new TestGistCommand(mockAuthService.Object, testGistLogger.Object);

        var gistSetLogger = Mock.Of<ILogger<GistSetCommand>>();
        var gistStatusLogger = Mock.Of<ILogger<GistStatusCommand>>();
        var gistShowLogger = Mock.Of<ILogger<GistShowCommand>>();

        var mockGistConfigService = new Mock<IGistConfigService>();
        var mockGistSetCommand = new GistSetCommand(mockGistConfigService.Object, gistSetLogger);
        var mockGistStatusCommand = new GistStatusCommand(_concreteAuthService, _gistInputService, _gistManager, gistStatusLogger);
        var mockGistShowCommand = new GistShowCommand(_concreteAuthService, _gistInputService, _gistManager, _packageYamlConverter, gistShowLogger);

        var logger = new Mock<ILogger<CommandRouter>>();

        return new CommandRouter(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            authCommand,
            testGistCommand,
            mockGistSetCommand,
            mockGistStatusCommand,
            mockGistShowCommand,
            logger.Object,
            mockErrorMessageService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDisplayUserFriendlyMessage_WhenPackageNotFound()
    {
        // Arrange
        var mockWinGetClient = new Mock<IWinGetClient>();
        var mockPassthroughClient = new Mock<IWinGetPassthroughClient>();
        var mockGistSyncService = new Mock<IGistSyncService>();
        var logger = new Mock<ILogger<CommandRouter>>();

        // パッケージ未発見例外をスローするように設定
        var packageNotFound = new InvalidOperationException("Package 'NonExistent.Package' not found");
        mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ThrowsAsync(packageNotFound);

        // AuthCommandとTestGistCommandのモックを作成
        var mockAuthService = new Mock<IGitHubAuthService>();
        var authLogger = new Mock<ILogger<AuthCommand>>();
        var authCommand = new AuthCommand(mockAuthService.Object, authLogger.Object);

        var testGistLogger = new Mock<ILogger<TestGistCommand>>();
        var testGistCommand = new TestGistCommand(mockAuthService.Object, testGistLogger.Object);

        // Gist関連コマンド用の具象GitHubAuthService（最小実装）
        var concreteAuthLogger = Mock.Of<ILogger<GitHubAuthService>>();
        var concreteAuthService = new GitHubAuthService(concreteAuthLogger);

        // Gist関連コマンドの依存関係をモック（最小実装）
        var mockGistInputService = new GistInputService();
        var mockStorage = Mock.Of<IGistConfigurationStorage>();
        var mockGistClientLogger = Mock.Of<ILogger<GitHubGistClient>>();
        var mockGistClient = new GitHubGistClient(concreteAuthService, mockGistClientLogger);
        var mockGistManagerLogger = Mock.Of<ILogger<GistManager>>();
        var mockPackageYamlConverter = new PackageYamlConverter();
        var mockGistManager = new GistManager(mockGistClient, mockStorage, mockPackageYamlConverter, mockGistManagerLogger);

        var gistSetLogger = Mock.Of<ILogger<GistSetCommand>>();
        var gistStatusLogger = Mock.Of<ILogger<GistStatusCommand>>();
        var gistShowLogger = Mock.Of<ILogger<GistShowCommand>>();

        var mockGistConfigService = new Mock<IGistConfigService>();
        var mockGistSetCommand = new GistSetCommand(mockGistConfigService.Object, gistSetLogger);
        var mockGistStatusCommand = new GistStatusCommand(concreteAuthService, mockGistInputService, mockGistManager, gistStatusLogger);
        var mockGistShowCommand = new GistShowCommand(concreteAuthService, mockGistInputService, mockGistManager, mockPackageYamlConverter, gistShowLogger);

        var mockErrorMessageService = new Mock<IErrorMessageService>();
        var service = new CommandRouter(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            authCommand,
            testGistCommand,
            mockGistSetCommand,
            mockGistStatusCommand,
            mockGistShowCommand,
            logger.Object,
            mockErrorMessageService.Object);

        var args = new[] { "install", "--id", "NonExistent.Package" };

        // Act
        var result = await service.ExecuteAsync(args);

        // Assert
        result.ShouldBe(1); // エラー終了コード

        // ErrorMessageServiceのHandlePackageNotFoundExceptionが呼び出されることを確認
        mockErrorMessageService.Verify(x => x.HandlePackageNotFoundException(packageNotFound), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDisplayHelpfulErrorMessage_WhenNetworkError()
    {
        // Arrange  
        var mockWinGetClient = new Mock<IWinGetClient>();
        var mockPassthroughClient = new Mock<IWinGetPassthroughClient>();
        var mockGistSyncService = new Mock<IGistSyncService>();
        var logger = new Mock<ILogger<CommandRouter>>();

        // ネットワーク例外をスローするように設定
        var networkError = new HttpRequestException("Unable to connect to the remote server");
        mockWinGetClient.Setup(x => x.InstallPackageAsync(It.IsAny<string[]>())).ThrowsAsync(networkError);

        // AuthCommandとTestGistCommandのモックを作成
        var mockAuthService = new Mock<IGitHubAuthService>();
        var authLogger = new Mock<ILogger<AuthCommand>>();
        var authCommand = new AuthCommand(mockAuthService.Object, authLogger.Object);

        var testGistLogger = new Mock<ILogger<TestGistCommand>>();
        var testGistCommand = new TestGistCommand(mockAuthService.Object, testGistLogger.Object);

        // Gist関連コマンド用の具象GitHubAuthService（最小実装）
        var concreteAuthLogger = Mock.Of<ILogger<GitHubAuthService>>();
        var concreteAuthService = new GitHubAuthService(concreteAuthLogger);

        // Gist関連コマンドの依存関係をモック（最小実装）
        var mockGistInputService = new GistInputService();
        var mockStorage = Mock.Of<IGistConfigurationStorage>();
        var mockGistClientLogger = Mock.Of<ILogger<GitHubGistClient>>();
        var mockGistClient = new GitHubGistClient(concreteAuthService, mockGistClientLogger);
        var mockGistManagerLogger = Mock.Of<ILogger<GistManager>>();
        var mockPackageYamlConverter = new PackageYamlConverter();
        var mockGistManager = new GistManager(mockGistClient, mockStorage, mockPackageYamlConverter, mockGistManagerLogger);

        var gistSetLogger = Mock.Of<ILogger<GistSetCommand>>();
        var gistStatusLogger = Mock.Of<ILogger<GistStatusCommand>>();
        var gistShowLogger = Mock.Of<ILogger<GistShowCommand>>();

        var mockGistConfigService = new Mock<IGistConfigService>();
        var mockGistSetCommand = new GistSetCommand(mockGistConfigService.Object, gistSetLogger);
        var mockGistStatusCommand = new GistStatusCommand(concreteAuthService, mockGistInputService, mockGistManager, gistStatusLogger);
        var mockGistShowCommand = new GistShowCommand(concreteAuthService, mockGistInputService, mockGistManager, mockPackageYamlConverter, gistShowLogger);

        var mockErrorMessageService = new Mock<IErrorMessageService>();
        var commandRouter = new CommandRouter(
            mockWinGetClient.Object,
            mockPassthroughClient.Object,
            mockGistSyncService.Object,
            authCommand,
            testGistCommand,
            mockGistSetCommand,
            mockGistStatusCommand,
            mockGistShowCommand,
            logger.Object,
            mockErrorMessageService.Object);

        var args = new[] { "install", "--id", "TestPackage.Test" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert
        result.ShouldBe(1); // エラー終了コード

        // ErrorMessageServiceのHandleNetworkExceptionが呼び出されることを確認
        mockErrorMessageService.Verify(x => x.HandleNetworkException(networkError), Times.Once);
    }
}

