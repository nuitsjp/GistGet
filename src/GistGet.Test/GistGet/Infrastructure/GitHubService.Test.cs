using Octokit;
using Shouldly;
using Moq;

namespace GistGet.Infrastructure;

public class GitHubServiceTests
{
    protected const string GitHubTarget = "git:https://github.com";
    protected readonly ICredentialService CredentialService = new CredentialService();
    protected readonly Mock<IConsoleService> ConsoleServiceMock = new();

    protected GitHubService CreateTarget()
    {
        return new GitHubService(CredentialService, ConsoleServiceMock.Object);
    }

    protected sealed class TestableGitHubService : GitHubService
    {
        public TestableGitHubService(ICredentialService credentialService, IConsoleService consoleService)
            : base(credentialService, consoleService)
        {
        }

        public OauthDeviceFlowRequest ExposeCreateDeviceFlowRequest()
        {
            return CreateDeviceFlowRequest();
        }
    }

    protected async Task<(GitHubClient Client, string Token, Gist Gist)?> CreateIsolatedGistAsync(string fileName, string description, string initialContent)
    {
        if (!CredentialService.TryGetCredential(GitHubTarget, out var credential) || string.IsNullOrEmpty(credential.Token))
        {
            // Skip test if no credential
            return null;
        }

        var client = new GitHubClient(new ProductHeaderValue("GistGet.Tests"))
        {
            Credentials = new Credentials(credential.Token)
        };

        var newGist = new NewGist
        {
            Description = description,
            Public = false,
            Files = { { fileName, initialContent } }
        };
        try
        {
            var created = await client.Gist.Create(newGist);
            return (client, credential.Token, created);
        }
        catch (ApiException)
        {
            // Skip test if API call fails (e.g. invalid token or permissions)
            return null;
        }
    }

    protected async Task DeleteGistQuietlyAsync(GitHubClient client, string gistId)
    {
        try
        {
            await client.Gist.Delete(gistId);
        }
        catch
        {
            // ignore cleanup failures
        }
    }

    public class SavePackagesAsync : GitHubServiceTests
    {
        [Fact]
        public async Task WithExistingGist_UpdatesYamlFile()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var fileName = $"gistget-packages-test-{Guid.NewGuid():N}.yaml";
            var description = $"GistGet Packages Test {Guid.NewGuid():N}";
            var initialYaml = "initial: {}\n";
            var result = await CreateIsolatedGistAsync(fileName, description, initialYaml);
            if (result == null) return;
            var (client, token, gist) = result.Value;
            var target = CreateTarget();

            var packages = new List<GistGetPackage>
            {
                new() { Id = "Foo.Bar", Version = "1.2.3", Silent = true },
                new() { Id = "Baz.Qux", Scope = "user" }
            };

            try
            {
                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                await target.SavePackagesAsync(token, gist.HtmlUrl, fileName, description, packages);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                var updated = await client.Gist.Get(gist.Id);
                updated.Files.ContainsKey(fileName).ShouldBeTrue();
                updated.Files[fileName].Content.ShouldContain("Foo.Bar");
                updated.Files[fileName].Content.ShouldContain("Baz.Qux");
            }
            finally
            {
                await DeleteGistQuietlyAsync(client, gist.Id);
            }
        }
    }

    public class GetPackagesAsync : GitHubServiceTests
    {
        [Fact]
        public async Task WithGistUrl_ReturnsPackagesFromYaml()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var fileName = $"gistget-packages-test-{Guid.NewGuid():N}.yaml";
            var description = $"GistGet Packages Test {Guid.NewGuid():N}";
            var yaml = """
                       Foo.Bar:
                         version: "1.2.3"
                         uninstall: true
                         silent: true
                       Baz.Qux:
                         scope: user
                       """;

            var isolatedResult = await CreateIsolatedGistAsync(fileName, description, yaml);
            if (isolatedResult == null) return;
            var (client, token, gist) = isolatedResult.Value;
            var target = CreateTarget();

            try
            {
                // -------------------------------------------------------------------
                // Act
                // -------------------------------------------------------------------
                var result = await target.GetPackagesAsync(token, gist.HtmlUrl, fileName, description);

                // -------------------------------------------------------------------
                // Assert
                // -------------------------------------------------------------------
                result.Count.ShouldBe(2);
                result.ShouldContain(p => p.Id == "Foo.Bar" && p.Version == "1.2.3" && p.Uninstall && p.Silent);
                result.ShouldContain(p => p.Id == "Baz.Qux" && p.Scope == "user");
            }
            finally
            {
                await DeleteGistQuietlyAsync(client, gist.Id);
            }
        }
    }

    public class CreateDeviceFlowRequest : GitHubServiceTests
    {
        [Fact]
        public void Always_IncludesGistScope()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var target = new TestableGitHubService(CredentialService, ConsoleServiceMock.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var request = target.ExposeCreateDeviceFlowRequest();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            request.Scopes.ShouldContain("gist");
        }
    }
}
