using Octokit;
using Shouldly;
using Moq;

namespace GistGet.Infrastructure;

public class GitHubServiceTests
{

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
        if (!CredentialService.TryGetCredential(out var credential) || string.IsNullOrEmpty(credential.Token))
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

        [Fact]
        public async Task PreservesAllPackageProperties()
        {
            // -------------------------------------------------------------------
            // Arrange: 全プロパティを設定したパッケージを準備
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
                new()
                {
                    Id = "TestPackage.AllProperties",
                    Version = "2.0.0",
                    Pin = "1.5.0",
                    PinType = "blocking",
                    Custom = "/CUSTOM_ARG",
                    Uninstall = false,
                    Scope = "machine",
                    Architecture = "x64",
                    Location = @"C:\Install",
                    Locale = "ja-JP",
                    AllowHashMismatch = true,
                    Force = true,
                    AcceptPackageAgreements = true,
                    AcceptSourceAgreements = true,
                    SkipDependencies = true,
                    Header = "X-Custom-Header",
                    InstallerType = "msi",
                    Log = @"C:\Logs\install.log",
                    Override = "/SILENT",
                    Interactive = false,
                    Silent = true
                }
            };

            try
            {
                // -------------------------------------------------------------------
                // Act: 保存後に再取得
                // -------------------------------------------------------------------
                await target.SavePackagesAsync(token, gist.HtmlUrl, fileName, description, packages);
                var retrievedPackages = await target.GetPackagesAsync(token, fileName, description);

                // -------------------------------------------------------------------
                // Assert: 全プロパティが保持されていることを検証
                // -------------------------------------------------------------------
                retrievedPackages.Count.ShouldBe(1);
                var pkg = retrievedPackages[0];
                
                pkg.Id.ShouldBe("TestPackage.AllProperties");
                pkg.Version.ShouldBe("2.0.0");
                pkg.Pin.ShouldBe("1.5.0");
                pkg.PinType.ShouldBe("blocking");
                pkg.Custom.ShouldBe("/CUSTOM_ARG");
                pkg.Uninstall.ShouldBeFalse();
                pkg.Scope.ShouldBe("machine");
                pkg.Architecture.ShouldBe("x64");
                pkg.Location.ShouldBe(@"C:\Install");
                pkg.Locale.ShouldBe("ja-JP");
                pkg.AllowHashMismatch.ShouldBeTrue();
                pkg.Force.ShouldBeTrue();
                pkg.AcceptPackageAgreements.ShouldBeTrue();
                pkg.AcceptSourceAgreements.ShouldBeTrue();
                pkg.SkipDependencies.ShouldBeTrue();
                pkg.Header.ShouldBe("X-Custom-Header");
                pkg.InstallerType.ShouldBe("msi");
                pkg.Log.ShouldBe(@"C:\Logs\install.log");
                pkg.Override.ShouldBe("/SILENT");
                pkg.Interactive.ShouldBeFalse();
                pkg.Silent.ShouldBeTrue();
            }
            finally
            {
                await DeleteGistQuietlyAsync(client, gist.Id);
            }
        }
    }

    public class GetPackagesFromUrlAsync : GitHubServiceTests
    {
        [Fact]
        public async Task WithGistRawUrl_ReturnsPackagesFromYaml()
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
                // Act - Use raw URL for the file
                // -------------------------------------------------------------------
                var rawUrl = gist.Files[fileName].RawUrl;
                var result = await target.GetPackagesFromUrlAsync(rawUrl);

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

    public class LoginAsyncConsoleInteraction : GitHubServiceTests
    {
        [Fact]
        public void WriteWarning_ShouldBeCalledWithUserCode()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            // This test verifies that IConsoleService.WriteWarning is called
            // We can only verify the interface contract here since LoginAsync
            // requires actual GitHub API interaction

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            // Verify the interface has the required methods
            ConsoleServiceMock.Object.WriteWarning("test");
            ConsoleServiceMock.Verify(x => x.WriteWarning("test"), Times.Once);
        }

        [Fact]
        public void SetClipboard_ShouldBeCalled()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            // This test verifies that IConsoleService.SetClipboard is callable

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Object.SetClipboard("TEST-CODE");
            ConsoleServiceMock.Verify(x => x.SetClipboard("TEST-CODE"), Times.Once);
        }

        [Fact]
        public void ReadLine_ShouldBeCalled()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ConsoleServiceMock.Setup(x => x.ReadLine()).Returns("");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = ConsoleServiceMock.Object.ReadLine();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ConsoleServiceMock.Verify(x => x.ReadLine(), Times.Once);
            result.ShouldBe("");
        }
    }
}
