using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NuitsJp.GistGet.Business.Services;
using NuitsJp.GistGet.Infrastructure.WinGet;
using NuitsJp.GistGet.Presentation;
using NuitsJp.GistGet.Tests.Mocks;
using Shouldly;
using Xunit;

namespace NuitsJp.GistGet.Tests.Presentation;

/// <summary>
/// CommandRouterのPresentation層テスト（t-wada式TDD対応）
/// UI制御・ルーティング・終了コードの検証に特化
/// Business層は完全にモック化
/// </summary>
public class CommandRouterTests
{
    private readonly MockWinGetClient _mockWinGetClient;
    private readonly MockWinGetPassthroughClient _mockPassthroughClient;
    private readonly MockGistSyncService _mockGistSyncService;
    private readonly Mock<IErrorMessageService> _mockErrorMessageService;
    private readonly Mock<ILogger<CommandRouter>> _mockLogger;

    public CommandRouterTests()
    {
        // Presentation層テスト専用: Business層をモック化し、UI制御のみをテスト
        _mockWinGetClient = new MockWinGetClient();
        _mockPassthroughClient = new MockWinGetPassthroughClient();
        _mockGistSyncService = new MockGistSyncService();
        _mockErrorMessageService = new Mock<IErrorMessageService>();
        _mockLogger = new Mock<ILogger<CommandRouter>>();
    }

    private CommandRouter CreateCommandRouterForRoutingTests()
    {
        // CommandRouterのルーティングロジックのみをテストするため、
        // 全てのCommandをnullにしてルーティング前の動作を検証する
        return new CommandRouter(
            _mockWinGetClient,
            _mockPassthroughClient,
            _mockGistSyncService,
            null!, // authCommand - ルーティングテストでは使用しない
            null!, // testGistCommand - ルーティングテストでは使用しない
            null!, // gistSetCommand - ルーティングテストでは使用しない
            null!, // gistStatusCommand - ルーティングテストでは使用しない
            null!, // gistShowCommand - ルーティングテストでは使用しない
            _mockLogger.Object,
            _mockErrorMessageService.Object);
    }

    #region COM Client Routing Tests (UI Control)

    [Theory]
    [InlineData("install")]
    [InlineData("uninstall")]
    [InlineData("upgrade")]
    public async Task ExecuteAsync_ShouldRouteToCOMClient_WhenUsingCOMCommands(string command)
    {
        // Arrange - Presentation層: 引数とルーティング制御のテスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var args = new[] { command, "--id", "TestPackage" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: COM Clientが正しく呼ばれること
        result.ShouldBe(0);
        VerifyCOMClientMethodCalled(command, args);
        // 他のサービスは呼ばれない
        _mockGistSyncService.LastCommand.ShouldBeNull();
        _mockPassthroughClient.LastArgs.ShouldBeNull();
    }

    private void VerifyCOMClientMethodCalled(string command, string[] args)
    {
        _mockWinGetClient.InitializeCalled.ShouldBeTrue();
        _mockWinGetClient.LastCommand.ShouldBe(command);
        _mockWinGetClient.LastArgs.ShouldBe(args);
    }

    #endregion

    #region Passthrough Routing Tests (UI Control)

    [Theory]
    [InlineData("list")]
    [InlineData("search")]
    [InlineData("show")]
    [InlineData("export")]
    [InlineData("import")]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenUsingPassthroughCommands(string command)
    {
        // Arrange - Presentation層: パススルーコマンドのルーティング制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var args = new[] { command, "test-arg" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: パススルークライアントが呼ばれること
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
        // 他のサービスは呼ばれない
        _mockGistSyncService.LastCommand.ShouldBeNull();
        _mockWinGetClient.LastCommand.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenNoArgs()
    {
        // Arrange - Presentation層: 引数なしの場合のデフォルト動作テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var args = Array.Empty<string>();

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: デフォルトでパススルーに渡されること
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
        // 他のサービスは呼ばれない
        _mockGistSyncService.LastCommand.ShouldBeNull();
        _mockWinGetClient.LastCommand.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToPassthrough_WhenUnknownCommand()
    {
        // Arrange - Presentation層: 未知のコマンドの場合のデフォルト動作テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var args = new[] { "unknown-command", "test-arg" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: 未知のコマンドもパススルーに渡されること
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(args);
        // 他のサービスは呼ばれない
        _mockGistSyncService.LastCommand.ShouldBeNull();
        _mockWinGetClient.LastCommand.ShouldBeNull();
    }

    #endregion

    #region Gist Service Routing Tests (UI Control)

    [Theory]
    [InlineData("sync")]
    public async Task ExecuteAsync_ShouldRouteToGistService_WhenUsingGistCommands(string command)
    {
        // Arrange - Presentation層: Gistコマンドのルーティング制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var args = new[] { command };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: GistSyncServiceが正しく呼ばれること
        result.ShouldBe(0);
        _mockGistSyncService.LastCommand.ShouldBe(command);
        // 他のサービスは呼ばれない
        _mockWinGetClient.LastCommand.ShouldBeNull();
        _mockPassthroughClient.LastArgs.ShouldBeNull();
    }

    #endregion

    #region Error Handling Tests (UI Control - Exit Codes)

    [Fact]
    public async Task ExecuteAsync_ShouldReturnErrorCode_WhenCOMClientFails()
    {
        // Arrange - Presentation層: COM例外時の終了コード制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var comException = new System.Runtime.InteropServices.COMException("COM Error", -2147024891);
        _mockWinGetClient.ShouldThrowOnInstall = comException;
        var args = new[] { "install", "--id", "TestPackage" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: エラー終了コードとエラーハンドリング呼び出し
        result.ShouldBe(1);
        _mockErrorMessageService.Verify(x => x.HandleComException(comException), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnErrorCode_WhenPackageNotFound()
    {
        // Arrange - Presentation層: パッケージ未発見時の終了コード制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var packageNotFound = new InvalidOperationException("Package not found");
        _mockWinGetClient.ShouldThrowOnInstall = packageNotFound;
        var args = new[] { "install", "--id", "NonExistent.Package" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: エラー終了コードとエラーハンドリング呼び出し
        result.ShouldBe(1);
        _mockErrorMessageService.Verify(x => x.HandlePackageNotFoundException(packageNotFound), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnErrorCode_WhenNetworkError()
    {
        // Arrange - Presentation層: ネットワークエラー時の終了コード制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var networkError = new HttpRequestException("Network error");
        _mockWinGetClient.ShouldThrowOnInstall = networkError;
        var args = new[] { "install", "--id", "TestPackage" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: エラー終了コードとエラーハンドリング呼び出し
        result.ShouldBe(1);
        _mockErrorMessageService.Verify(x => x.HandleNetworkException(networkError), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnErrorCode_WhenBusinessLayerFails()
    {
        // Arrange - Presentation層: Business層失敗時の終了コード制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        _mockWinGetClient.ShouldReturnErrorCode = 1;
        var args = new[] { "install", "--id", "TestPackage" };

        // Act
        var result = await commandRouter.ExecuteAsync(args);

        // Assert - UI制御: Business層のエラーコードが正しく伝播されること
        result.ShouldBe(1);
    }

    #endregion

    #region Input Processing Tests (UI Control)

    [Theory]
    [InlineData("install", "--id", "TestPackage", "--force")]
    [InlineData("uninstall", "--id", "TestPackage", "--silent")]
    [InlineData("upgrade", "--id", "TestPackage", "--include-unknown")]
    public async Task ExecuteAsync_ShouldPassArgumentsCorrectly_ToCOMClient(string command, params string[] additionalArgs)
    {
        // Arrange - Presentation層: 引数処理とパラメータ渡しの制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var allArgs = new[] { command }.Concat(additionalArgs).ToArray();

        // Act
        var result = await commandRouter.ExecuteAsync(allArgs);

        // Assert - UI制御: 引数が正しく渡されること
        result.ShouldBe(0);
        _mockWinGetClient.LastArgs.ShouldBe(allArgs);
    }

    [Theory]
    [InlineData("list", "--name", "git")]
    [InlineData("search", "visual")]
    [InlineData("show", "--id", "Microsoft.VisualStudioCode")]
    public async Task ExecuteAsync_ShouldPassArgumentsCorrectly_ToPassthrough(string command, params string[] additionalArgs)
    {
        // Arrange - Presentation層: パススルー引数処理の制御テスト
        var commandRouter = CreateCommandRouterForRoutingTests();
        var allArgs = new[] { command }.Concat(additionalArgs).ToArray();

        // Act
        var result = await commandRouter.ExecuteAsync(allArgs);

        // Assert - UI制御: 引数が正しく渡されること
        result.ShouldBe(0);
        _mockPassthroughClient.LastArgs.ShouldBe(allArgs);
    }

    #endregion
}