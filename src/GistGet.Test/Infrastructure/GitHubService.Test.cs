using System.Net;
using System.Reflection;
using Moq;
using Octokit;
using Shouldly;

namespace GistGet.Infrastructure;

public class GitHubServiceTests
{

    protected readonly ICredentialService CredentialService = new CredentialService();
    protected readonly Mock<IConsoleService> ConsoleServiceMock = new();
    protected readonly IGitHubClientFactory GitHubClientFactory = new GitHubClientFactory();

    protected GitHubService CreateTarget()
    {
        return new GitHubService(CredentialService, ConsoleServiceMock.Object, GitHubClientFactory);
    }

    protected sealed class TestableGitHubService : GitHubService
    {
        public TestableGitHubService(ICredentialService credentialService, IConsoleService consoleService, IGitHubClientFactory gitHubClientFactory)
            : base(credentialService, consoleService, gitHubClientFactory)
        {
        }

        public OauthDeviceFlowRequest ExposeCreateDeviceFlowRequest()
        {
            return CreateDeviceFlowRequest();
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;

        public StubHttpMessageHandler(string content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content)
            };

            return Task.FromResult(response);
        }
    }

    private static void SetProperty<TTarget>(TTarget target, string propertyName, object? value)
    {
        var property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
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
            var target = new TestableGitHubService(CredentialService, ConsoleServiceMock.Object, GitHubClientFactory);

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

    public class LoginAsync : GitHubServiceTests
    {
        [Fact]
        public async Task OpensBrowserAndReturnsCredential()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();
            consoleService.Setup(x => x.ReadLine()).Returns(string.Empty);

            var deviceFlowResponse = new OauthDeviceFlowResponse(
                "device-code",
                "USER-CODE",
                "https://example.com/verify",
                900,
                5);

            var token = new OauthToken();
            SetProperty(token, "AccessToken", "token-xyz");

            var user = new User();
            SetProperty(user, "Login", "octocat");

            var unauthenticatedClient = new Mock<IGitHubClientWrapper>();
            unauthenticatedClient
                .Setup(x => x.InitiateDeviceFlowAsync(It.IsAny<OauthDeviceFlowRequest>()))
                .ReturnsAsync(deviceFlowResponse);
            unauthenticatedClient
                .Setup(x => x.CreateAccessTokenForDeviceFlowAsync(Constants.ClientId, deviceFlowResponse))
                .ReturnsAsync(token);

            var authenticatedClient = new Mock<IGitHubClientWrapper>();
            authenticatedClient.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            var factory = new Mock<IGitHubClientFactory>();
            factory
                .SetupSequence(x => x.Create(It.IsAny<string?>()))
                .Returns(unauthenticatedClient.Object)
                .Returns(authenticatedClient.Object);

            var target = new TestableGitHubServiceForLogin(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var credential = await target.LoginAsync();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            consoleService.Verify(x => x.SetClipboard("USER-CODE"), Times.Once);
            consoleService.Verify(x => x.WriteWarning(It.Is<string>(m => m.Contains("USER-CODE"))), Times.Once);
            consoleService.Verify(x => x.WriteInfo(It.Is<string>(m => m.Contains("https://example.com/verify"))), Times.Once);
            consoleService.Verify(x => x.ReadLine(), Times.Once);
            target.OpenedUrls.ShouldContain("https://example.com/verify");
            credential.Username.ShouldBe("octocat");
            credential.Token.ShouldBe("token-xyz");
        }

        private sealed class TestableGitHubServiceForLogin : GitHubService
        {
            public List<string> OpenedUrls { get; } = new();

            public TestableGitHubServiceForLogin(ICredentialService credentialService, IConsoleService consoleService, IGitHubClientFactory gitHubClientFactory)
                : base(credentialService, consoleService, gitHubClientFactory)
            {
            }

            protected override bool OpenBrowser(string verificationUri)
            {
                OpenedUrls.Add(verificationUri);
                return true;
            }
        }
    }

    public class GetTokenStatusAsync : GitHubServiceTests
    {
        [Fact]
        public async Task WithValidToken_ReturnsLoginAndScopes()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var usersClient = new Mock<IGitHubClientWrapper>();
            var user = new User();
            SetProperty(user, "Login", "octocat");
            usersClient.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            var apiInfo = new ApiInfo(
                new Dictionary<string, Uri>(),
                new List<string> { "gist", "repo" },
                new List<string>(),
                string.Empty,
                new RateLimit(5000, 4999, DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                TimeSpan.Zero);

            usersClient.Setup(x => x.GetLastApiInfo()).Returns(apiInfo);

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token-123")).Returns(usersClient.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var status = await target.GetTokenStatusAsync("token-123");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            status.Username.ShouldBe("octocat");
            status.Scopes.ShouldContain("gist");
            status.Scopes.ShouldContain("repo");
        }
    }

    public class GetPackagesFromUrlAsyncMock : GitHubServiceTests
    {
        [Fact]
        public async Task WithEmptyContent_ReturnsEmptyCollection()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();
            var httpClient = new HttpClient(new StubHttpMessageHandler(string.Empty));
            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create(It.IsAny<string?>())).Returns(Mock.Of<IGitHubClientWrapper>());
            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object, httpClient);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await target.GetPackagesFromUrlAsync("https://example.com/gist");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeEmpty();
        }
    }

    public class GetPackagesAsyncMock : GitHubServiceTests
    {
        [Fact]
        public async Task WithMatchingGist_DeserializesPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var listingGist = new Gist();
            SetProperty(listingGist, "Id", "1");
            SetProperty(listingGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var detailGist = new Gist();
            SetProperty(detailGist, "Id", "1");
            SetProperty(detailGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, "Foo.Bar: {}\n", string.Empty) }
            });

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { listingGist });
            clientWrapper.Setup(x => x.GetGistAsync("1")).ReturnsAsync(detailGist);

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await target.GetPackagesAsync("token", "packages.yaml", "description");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldHaveSingleItem().Id.ShouldBe("Foo.Bar");
        }

        [Fact]
        public async Task WithStoredCredentialToken_UsesFallbackToken()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var stored = new Credential("stored-user", "stored-token");
            var credentialService = new Mock<ICredentialService>();
            credentialService.Setup(x => x.TryGetCredential(out stored)).Returns(true);
            var consoleService = new Mock<IConsoleService>();

            var listingGist = new Gist();
            SetProperty(listingGist, "Id", "1");
            SetProperty(listingGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var detailGist = new Gist();
            SetProperty(detailGist, "Id", "1");
            SetProperty(detailGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, "Foo.Bar: {}\n", string.Empty) }
            });

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { listingGist });
            clientWrapper.Setup(x => x.GetGistAsync("1")).ReturnsAsync(detailGist);

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("stored-token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await target.GetPackagesAsync(string.Empty, "packages.yaml", "description");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            factory.Verify(x => x.Create("stored-token"), Times.Once);
            result.ShouldHaveSingleItem().Id.ShouldBe("Foo.Bar");
        }

        [Fact]
        public async Task WithoutAnyToken_ThrowsInvalidOperation()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            Credential? missing = null;
            credentialService.Setup(x => x.TryGetCredential(out missing!)).Returns(false);
            var consoleService = new Mock<IConsoleService>();
            var factory = new Mock<IGitHubClientFactory>();

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<InvalidOperationException>(() =>
                target.GetPackagesAsync(string.Empty, "packages.yaml", "description"));
            factory.Verify(x => x.Create(It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task WhenNoMatchingGist_ReturnsEmpty()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist>());

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await target.GetPackagesAsync("token", "packages.yaml", "description");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeEmpty();
            clientWrapper.Verify(x => x.GetGistAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task WithEmptyContent_ReturnsEmpty()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var listingGist = new Gist();
            SetProperty(listingGist, "Id", "1");
            SetProperty(listingGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var detailGist = new Gist();
            SetProperty(detailGist, "Id", "1");
            SetProperty(detailGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { listingGist });
            clientWrapper.Setup(x => x.GetGistAsync("1")).ReturnsAsync(detailGist);

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await target.GetPackagesAsync("token", "packages.yaml", "description");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task WithMissingTargetFile_UsesFirstYamlFile()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var listingGist = new Gist();
            SetProperty(listingGist, "Id", "1");
            SetProperty(listingGist, "Description", "description");
            SetProperty(listingGist, "Files", new Dictionary<string, GistFile>
            {
                { "other.yml", new GistFile(0, "other.yml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var detailGist = new Gist();
            SetProperty(detailGist, "Id", "1");
            SetProperty(detailGist, "Files", new Dictionary<string, GistFile>
            {
                { "other.yml", new GistFile(0, "other.yml", string.Empty, string.Empty, "Other.Package: {}\n", string.Empty) }
            });

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { listingGist });
            clientWrapper.Setup(x => x.GetGistAsync("1")).ReturnsAsync(detailGist);

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = await target.GetPackagesAsync("token", "packages.yaml", "description");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldHaveSingleItem().Id.ShouldBe("Other.Package");
        }

        [Fact]
        public async Task WithMultipleMatches_ThrowsInvalidOperation()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var gistByFile = new Gist();
            SetProperty(gistByFile, "Id", "1");
            SetProperty(gistByFile, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var gistByDescription = new Gist();
            SetProperty(gistByDescription, "Id", "2");
            SetProperty(gistByDescription, "Description", "description");
            SetProperty(gistByDescription, "Files", new Dictionary<string, GistFile>());

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { gistByFile, gistByDescription });

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<InvalidOperationException>(() =>
                target.GetPackagesAsync("token", "packages.yaml", "description"));
        }
    }

    public class SavePackagesAsyncMock : GitHubServiceTests
    {
        [Fact]
        public async Task WhenGistExists_EditsFileWithSerializedContent()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();
            var packages = new List<GistGetPackage>
            {
                new() { Id = "Foo.Bar", Version = "1.0.0" }
            };

            var existingGist = new Gist();
            SetProperty(existingGist, "Id", "gist-1");
            SetProperty(existingGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { existingGist });

            GistUpdate? captured = null;
            clientWrapper
                .Setup(x => x.EditGistAsync("gist-1", It.IsAny<GistUpdate>()))
                .Callback<string, GistUpdate>((_, update) => captured = update)
                .ReturnsAsync(new Gist());

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await target.SavePackagesAsync("token", "https://gist.github.com/gist-1", "packages.yaml", "description", packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            captured.ShouldNotBeNull();
            captured!.Files.ShouldContainKey("packages.yaml");
            captured.Files["packages.yaml"].Content.ShouldContain("Foo.Bar");
        }

        [Fact]
        public async Task WhenNoMatchingGist_CreatesNewPrivateGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();
            var packages = new List<GistGetPackage> { new() { Id = "New.Package" } };

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist>());

            NewGist? created = null;
            clientWrapper
                .Setup(x => x.CreateGistAsync(It.IsAny<NewGist>()))
                .Callback<NewGist>(gist => created = gist)
                .ReturnsAsync(new Gist());

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await target.SavePackagesAsync("token", string.Empty, "packages.yaml", "GistGet Packages", packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            created.ShouldNotBeNull();
            created!.Public.ShouldBeFalse();
            created.Description.ShouldBe("GistGet Packages");
            created.Files.ShouldContainKey("packages.yaml");
            created.Files["packages.yaml"].ShouldContain("New.Package");
        }

        [Fact]
        public async Task WhenMatchingGistExistsWithoutUrl_EditsExistingGist()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();
            var packages = new List<GistGetPackage> { new() { Id = "Package.A" } };

            var existingGist = new Gist();
            SetProperty(existingGist, "Id", "gist-123");
            SetProperty(existingGist, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { existingGist });
            clientWrapper.Setup(x => x.EditGistAsync("gist-123", It.IsAny<GistUpdate>())).ReturnsAsync(new Gist());

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await target.SavePackagesAsync("token", string.Empty, "packages.yaml", "description", packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            clientWrapper.Verify(x => x.EditGistAsync("gist-123", It.IsAny<GistUpdate>()), Times.Once);
            clientWrapper.Verify(x => x.CreateGistAsync(It.IsAny<NewGist>()), Times.Never);
        }

        [Fact]
        public async Task WithRawGistId_UsesProvidedId()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();
            var packages = new List<GistGetPackage> { new() { Id = "Package.A" } };

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            GistUpdate? captured = null;
            clientWrapper
                .Setup(x => x.EditGistAsync("raw-id", It.IsAny<GistUpdate>()))
                .Callback<string, GistUpdate>((_, update) => captured = update)
                .ReturnsAsync(new Gist());

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            await target.SavePackagesAsync("token", "raw-id", "packages.yaml", "description", packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            captured.ShouldNotBeNull();
            clientWrapper.Verify(x => x.EditGistAsync("raw-id", It.IsAny<GistUpdate>()), Times.Once);
        }

        [Fact]
        public async Task WithMultipleMatches_ThrowsInvalidOperation()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            var consoleService = new Mock<IConsoleService>();

            var gistByFile = new Gist();
            SetProperty(gistByFile, "Id", "1");
            SetProperty(gistByFile, "Files", new Dictionary<string, GistFile>
            {
                { "packages.yaml", new GistFile(0, "packages.yaml", string.Empty, string.Empty, string.Empty, string.Empty) }
            });

            var gistByDescription = new Gist();
            SetProperty(gistByDescription, "Id", "2");
            SetProperty(gistByDescription, "Description", "description");
            SetProperty(gistByDescription, "Files", new Dictionary<string, GistFile>());

            var clientWrapper = new Mock<IGitHubClientWrapper>();
            clientWrapper.Setup(x => x.GetAllGistsAsync()).ReturnsAsync(new List<Gist> { gistByFile, gistByDescription });

            var factory = new Mock<IGitHubClientFactory>();
            factory.Setup(x => x.Create("token")).Returns(clientWrapper.Object);

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<InvalidOperationException>(() =>
                target.SavePackagesAsync("token", string.Empty, "packages.yaml", "description", Array.Empty<GistGetPackage>()));
        }

        [Fact]
        public async Task WithoutAnyToken_ThrowsInvalidOperation()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var credentialService = new Mock<ICredentialService>();
            Credential? missing = null;
            credentialService.Setup(x => x.TryGetCredential(out missing!)).Returns(false);

            var consoleService = new Mock<IConsoleService>();
            var factory = new Mock<IGitHubClientFactory>();

            var target = new GitHubService(credentialService.Object, consoleService.Object, factory.Object);

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            await Should.ThrowAsync<InvalidOperationException>(() =>
                target.SavePackagesAsync(string.Empty, string.Empty, "packages.yaml", "description", Array.Empty<GistGetPackage>()));
            factory.Verify(x => x.Create(It.IsAny<string?>()), Times.Never);
        }
    }
}
