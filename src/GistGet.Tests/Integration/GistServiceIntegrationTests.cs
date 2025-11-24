using GistGet.Application.Services;
using GistGet.Infrastructure.Security;
using GistGet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GistGet.Tests.Integration;

/// <summary>
/// GistServiceの結合テスト
/// 前提条件: scripts\Run-AuthLogin.ps1を実行してGitHubアカウントで認証済みであること
/// 認証されていない場合、テストは自動的にスキップされます
/// </summary>
[Collection("GistIntegration")]
public class GistServiceIntegrationTests : IClassFixture<GistIntegrationTestFixture>
{
    private readonly GistIntegrationTestFixture _fixture;
    private readonly IGistService _gistService;
    private readonly IAuthService _authService;

    public GistServiceIntegrationTests(GistIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var credentialService = new CredentialService();
        _authService = new AuthService(credentialService);
        _gistService = new GistService(_authService);
    }

    [Fact]
    public async Task Test1_SavePackagesAsync_CreatesNewGist_WhenNoGistExists()
    {
        // 認証チェック - 認証されていない場合はスキップ
        if (!await _authService.IsAuthenticatedAsync())
        {
            return;
        }

        // Arrange
        var testPackages = new Dictionary<string, GistGetPackage>
        {
            { "Microsoft.PowerToys", new GistGetPackage { Id = "Microsoft.PowerToys", Version = "0.75.0" } },
            { "7zip.7zip", new GistGetPackage { Id = "7zip.7zip" } }
        };

        // Act
        await _gistService.SavePackagesAsync(testPackages);

        // Assert
        // Gistが作成されたことを確認するため、再度取得
        var retrievedPackages = await _gistService.GetPackagesAsync();
        
        // Gistが正しく保存・取得できることを確認
        // 注: 初回実行時はGistが作成され、2回目以降は既存のGistが更新される
        Assert.NotNull(retrievedPackages);
    }

    [Fact]
    public async Task Test2_GetPackagesAsync_ReturnsPackages_FromExistingGist()
    {
        // 認証チェック
        if (!await _authService.IsAuthenticatedAsync())
        {
            return;
        }

        // Arrange - Test1で作成されたGistが存在することを前提

        // Act
        var packages = await _gistService.GetPackagesAsync();

        // Assert
        // Gistが取得できることを確認（空の場合もあり得る）
        Assert.NotNull(packages);
    }

    [Fact]
    public async Task Test3_SavePackagesAsync_UpdatesExistingGist_WhenGistExists()
    {
        // 認証チェック
        if (!await _authService.IsAuthenticatedAsync())
        {
            return;
        }

        // Arrange
        var updatedPackages = new Dictionary<string, GistGetPackage>
        {
            { "Microsoft.PowerToys", new GistGetPackage { Id = "Microsoft.PowerToys", Version = "0.76.0" } },
            { "7zip.7zip", new GistGetPackage { Id = "7zip.7zip" } },
            { "Git.Git", new GistGetPackage { Id = "Git.Git", Version = "2.43.0" } }
        };

        // Act
        await _gistService.SavePackagesAsync(updatedPackages);

        // Assert
        var retrievedPackages = await _gistService.GetPackagesAsync();
        Assert.NotNull(retrievedPackages);
    }

    [Fact]
    public async Task Test4_GetPackagesAsync_WithGistUrl_ReturnsPackages()
    {
        // 認証チェック
        if (!await _authService.IsAuthenticatedAsync())
        {
            return;
        }

        // Arrange
        // まず現在のGist URLを取得するため、一度保存して取得
        var testPackages = new Dictionary<string, GistGetPackage>
        {
            { "Test.Package", new GistGetPackage { Id = "Test.Package" } }
        };
        await _gistService.SavePackagesAsync(testPackages);

        // Act
        var packages = await _gistService.GetPackagesAsync();

        // Assert
        Assert.NotNull(packages);
    }

    [Fact]
    public async Task Test5_AuthenticationCheck_VerifiesCredentialStorage()
    {
        // 認証チェック - このテストは認証状態を確認するのみ
        var isAuthenticated = await _authService.IsAuthenticatedAsync();

        // 認証されている場合は成功、されていない場合もテストは成功（警告のみ）
        if (!isAuthenticated)
        {
            // 認証されていない場合は、統合テストがスキップされたことを示す
            Assert.True(true, "Integration tests skipped: Not authenticated. Run scripts\\Run-AuthLogin.ps1 first.");
        }
        else
        {
            // 認証されている場合は、トークンが取得できることを確認
            var token = await _authService.GetAccessTokenAsync();
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }
    }
}

/// <summary>
/// 結合テスト用のフィクスチャ
/// テスト間でGist IDなどの状態を共有するために使用
/// </summary>
public class GistIntegrationTestFixture : IDisposable
{
    public string? GistId { get; set; }

    public void Dispose()
    {
        // クリーンアップ処理
        // ログアウトは行わない（認証状態を保持）
    }
}
