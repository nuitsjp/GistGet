using GistGet.Application.Services;
using GistGet.Infrastructure.Security;
using GistGet.Models;
using Octokit;
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

    public GistServiceIntegrationTests(GistIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveAndGetPackagesAsync_WithCustomFileNameAndDescription_UsesIsolatedRandomGist()
    {
        if (!await _fixture.IsAuthenticatedAsync())
        {
            return;
        }

        var (fileName, description) = _fixture.CreateUniqueGistMetadata();
        var testPackages = new Dictionary<string, GistGetPackage>
        {
            { "Test.Package", new GistGetPackage { Id = "Test.Package", Version = "1.2.3" } },
            { "Another.Package", new GistGetPackage { Id = "Another.Package" } }
        };

        string? createdGistId = null;
        try
        {
            await _fixture.GistService.SavePackagesAsync(testPackages, fileName, description);

            createdGistId = await _fixture.FindGistIdAsync(fileName, description);
            Assert.False(string.IsNullOrEmpty(createdGistId), "Gist was not created.");

            var retrievedPackages = await _fixture.GistService.GetPackagesAsync(null, fileName, description);

            Assert.NotNull(createdGistId);
            Assert.Equal(testPackages.Keys.OrderBy(x => x), retrievedPackages.Keys.OrderBy(x => x));
            var package = retrievedPackages["Test.Package"];
            Assert.Equal("Test.Package", package.Id);
            Assert.Equal("1.2.3", package.Version);
        }
        finally
        {
            await _fixture.DeleteGistIfExistsAsync(createdGistId);
        }
    }
}

/// <summary>
/// 結合テスト用のフィクスチャ
/// テスト間でGitHubへのアクセストークンや作成したGistのクリーンアップを担う
/// </summary>
public class GistIntegrationTestFixture : IDisposable
{
    private readonly IAuthService _authService;
    public IGistService GistService { get; }

    public GistIntegrationTestFixture()
    {
        var credentialService = new CredentialService();
        _authService = new AuthService(credentialService);
        GistService = new GistService(_authService);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _authService.IsAuthenticatedAsync();
    }

    public (string FileName, string Description) CreateUniqueGistMetadata()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return ($"gistget-test-{suffix}.yaml", $"GistGet Test {suffix}");
    }

    public async Task<string?> FindGistIdAsync(string fileName, string description)
    {
        var client = await CreateGitHubClientAsync();
        if (client == null)
        {
            return null;
        }

        for (var i = 0; i < 3; i++)
        {
            var gists = await client.Gist.GetAll();
            var target = gists.FirstOrDefault(g => g.Description == description || g.Files.ContainsKey(fileName));
            if (target != null)
            {
                return target.Id;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return null;
    }

    public async Task DeleteGistIfExistsAsync(string? gistId)
    {
        if (string.IsNullOrEmpty(gistId))
        {
            return;
        }

        var client = await CreateGitHubClientAsync();
        if (client == null)
        {
            return;
        }

        await client.Gist.Delete(gistId);
    }

    public void Dispose()
    {
    }

    private async Task<GitHubClient?> CreateGitHubClientAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var client = new GitHubClient(new ProductHeaderValue("GistGet"))
        {
            Credentials = new Credentials(token)
        };
        return client;
    }
}
