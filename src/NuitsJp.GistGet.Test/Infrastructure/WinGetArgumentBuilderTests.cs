using NuitsJp.GistGet.Infrastructure;
using Shouldly;

namespace NuitsJp.GistGet.Test.Infrastructure;

public class WinGetArgumentBuilderTests
{
    private readonly WinGetArgumentBuilder _builder = new();

    public class BuildInstallArgsFromOptions : WinGetArgumentBuilderTests
    {
        [Fact]
        public void WithRequiredProperties_ReturnsMinimalArgs()
        {
            // Arrange
            var options = new InstallOptions { Id = "Test.Package" };

            // Act
            var args = _builder.BuildInstallArgs(options);

            // Assert
            args.ShouldBe(new[] { "install", "--id", "Test.Package" });
        }

        [Fact]
        public void WithAllProperties_ReturnsAllArgs()
        {
            // Arrange
            var options = new InstallOptions
            {
                Id = "Test.Package",
                Version = "1.2.3",
                Scope = "user",
                Architecture = "x64",
                Location = "C:\\Install",
                Interactive = true,
                Silent = true,
                Log = "install.log",
                Override = "/custom",
                Force = true,
                SkipDependencies = true,
                Header = "X-Test: 1",
                InstallerType = "msi",
                Custom = "--custom-arg",
                Locale = "en-US",
                AcceptPackageAgreements = true,
                AcceptSourceAgreements = true,
                AllowHashMismatch = true
            };

            // Act
            var args = _builder.BuildInstallArgs(options);

            // Assert
            // Note: The order of arguments depends on implementation, but checking for containment is safer for robustness,
            // though strict ordering in implementation makes testing exact sequence easier.
            // Here we check for presence of all expected flags.
            var expectedArgs = new[]
            {
                "install", "--id", "Test.Package",
                "--version", "1.2.3",
                "--scope", "user",
                "--architecture", "x64",
                "--location", "C:\\Install",
                "--interactive",
                "--silent",
                "--log", "install.log",
                "--override", "/custom",
                "--force",
                "--skip-dependencies",
                "--header", "X-Test: 1",
                "--installer-type", "msi",
                "--custom", "--custom-arg",
                "--locale", "en-US",
                "--accept-package-agreements",
                "--accept-source-agreements",
                "--ignore-security-hash"
            };

            args.ShouldBeSubsetOf(expectedArgs); // Basic check
            args.Length.ShouldBe(expectedArgs.Length); // Ensure no missing or extra args

            // To ideally test specific pairs like "--id" "Test.Package", we can check sequentially or finding indices.
            // For this test, let's assume the builder produces them in a deterministic order and check containment.
        }
    }

    public class BuildUpgradeArgsFromOptions : WinGetArgumentBuilderTests
    {
        [Fact]
        public void WithAllProperties_ReturnsAllArgs()
        {
            var options = new UpgradeOptions
            {
                Id = "Test.Package",
                Version = "2.0.0",
                Scope = "machine",
                Header = "X-Custom: header",
                Force = true
            };

            var args = _builder.BuildUpgradeArgs(options);

            var expectedArgs = new[]
            {
                "upgrade", "--id", "Test.Package",
                "--version", "2.0.0",
                "--scope", "machine",
                "--header", "X-Custom: header",
                "--force"
            };

            args.ShouldBeSubsetOf(expectedArgs);
            args.Length.ShouldBe(expectedArgs.Length);
        }

        [Fact]
        public void WithExtendedOptions_IncludesNewFlags()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var options = new UpgradeOptions
            {
                Id = "Test.Package",
                Manifest = "C:\\manifests\\app.yaml",
                Name = "Sample App",
                Moniker = "sample-app",
                AuthenticationMode = "silentPreferred",
                AuthenticationAccount = "user@example.com",
                IgnoreLocalArchiveMalwareScan = true,
                Wait = true,
                OpenLogs = true,
                VerboseLogs = true,
                IgnoreWarnings = true,
                DisableInteractivity = true,
                Proxy = "http://proxy:8080",
                NoProxy = true
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildUpgradeArgs(options);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldContain("--manifest");
            args.ShouldContain("C:\\manifests\\app.yaml");
            args.ShouldContain("--name");
            args.ShouldContain("Sample App");
            args.ShouldContain("--moniker");
            args.ShouldContain("sample-app");
            args.ShouldContain("--authentication-mode");
            args.ShouldContain("silentPreferred");
            args.ShouldContain("--authentication-account");
            args.ShouldContain("user@example.com");
            args.ShouldContain("--ignore-local-archive-malware-scan");
            args.ShouldContain("--wait");
            args.ShouldContain("--logs");
            args.ShouldContain("--verbose");
            args.ShouldContain("--nowarn");
            args.ShouldContain("--disable-interactivity");
            args.ShouldContain("--proxy");
            args.ShouldContain("http://proxy:8080");
            args.ShouldContain("--no-proxy");
        }
    }

    public class BuildUninstallArgsFromOptions : WinGetArgumentBuilderTests
    {
        [Fact]
        public void WithAllProperties_ReturnsAllArgs()
        {
            var options = new UninstallOptions
            {
                Id = "Test.Package",
                Silent = true,
                Scope = "user"
            };

            var args = _builder.BuildUninstallArgs(options);

            var expectedArgs = new[]
            {
                "uninstall", "--id", "Test.Package",
                "--silent",
                "--scope", "user"
            };

            args.ShouldBeSubsetOf(expectedArgs);
            args.Length.ShouldBe(expectedArgs.Length);
        }
    }

    public class BuildPinAddArgsFromParams : WinGetArgumentBuilderTests
    {
        [Fact]
        public void WithPinTypeAndForce_ReturnsAllArgs()
        {
            var args = _builder.BuildPinAddArgs("Test.Package", "1.2.*", "blocking", true);

            var expectedArgs = new[]
            {
                "pin", "add", "--id", "Test.Package",
                "--version", "1.2.*",
                "--blocking",
                "--force"
            };

            args.ShouldBeSubsetOf(expectedArgs);
            args.Length.ShouldBe(expectedArgs.Length);
        }

        [Fact]
        public void WithGatingPinType_ReturnsGatingArg()
        {
            var args = _builder.BuildPinAddArgs("Test.Package", "1.2.*", "gating");

            var expectedArgs = new[]
            {
                "pin", "add", "--id", "Test.Package",
                "--version", "1.2.*",
                "--gating"
            };

            args.ShouldBeSubsetOf(expectedArgs);
            args.Length.ShouldBe(expectedArgs.Length);
        }

        [Fact]
        public void WithoutPinTypeOrForce_ReturnsMinimalArgs()
        {
            // -------------------------------------------------------------------
            // Arrange & Act
            // -------------------------------------------------------------------
            var args = _builder.BuildPinAddArgs("Test.Package", "1.0.0");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            var expectedArgs = new[]
            {
                "pin", "add", "--id", "Test.Package",
                "--version", "1.0.0"
            };

            args.ShouldBeSubsetOf(expectedArgs);
            args.Length.ShouldBe(expectedArgs.Length);
            args.ShouldNotContain("--blocking");
            args.ShouldNotContain("--gating");
            args.ShouldNotContain("--force");
        }

        [Fact]
        public void WithUnknownPinType_DoesNotAddTypeFlag()
        {
            // -------------------------------------------------------------------
            // Arrange & Act
            // -------------------------------------------------------------------
            var args = _builder.BuildPinAddArgs("Test.Package", "1.0.0", "unknown");

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldNotContain("--blocking");
            args.ShouldNotContain("--gating");
            args.ShouldNotContain("unknown");
            args.ShouldContain("pin");
            args.ShouldContain("add");
            args.ShouldContain("--id");
            args.ShouldContain("Test.Package");
        }
    }

    public class BuildInstallArgsFromGistGetPackage : WinGetArgumentBuilderTests
    {
        [Fact]
        public void WithPinAndVersion_PrefersPinOverVersion()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage { Id = "Test.Pkg", Pin = "1.2.3", Version = "1.0.0" };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildInstallArgs(package);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldContain("1.2.3");
            args.ShouldNotContain("1.0.0");
        }

        [Fact]
        public void WithVersionOnly_UsesVersion()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage { Id = "Test.Pkg", Version = "2.0.0" };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildInstallArgs(package);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldContain("--version");
            args.ShouldContain("2.0.0");
        }

        [Fact]
        public void WithAllCommonOptions_BuildsCompleteArgs()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage
            {
                Id = "Test.Package",
                Pin = "1.5.0",
                Scope = "machine",
                Architecture = "x86",
                Location = "D:\\Apps",
                Interactive = true,
                Silent = true,
                Log = "install.log",
                Override = "/quiet",
                Force = true,
                SkipDependencies = true,
                Header = "X-Custom",
                InstallerType = "exe",
                Custom = "--extra",
                Locale = "en-GB",
                AcceptPackageAgreements = true,
                AcceptSourceAgreements = true,
                AllowHashMismatch = true
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildInstallArgs(package);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldContain("install");
            args.ShouldContain("--id");
            args.ShouldContain("Test.Package");
            args.ShouldContain("--version");
            args.ShouldContain("1.5.0");
            args.ShouldContain("--scope");
            args.ShouldContain("machine");
            args.ShouldContain("--architecture");
            args.ShouldContain("x86");
            args.ShouldContain("--location");
            args.ShouldContain("D:\\Apps");
            args.ShouldContain("--interactive");
            args.ShouldContain("--silent");
            args.ShouldContain("--log");
            args.ShouldContain("install.log");
            args.ShouldContain("--override");
            args.ShouldContain("/quiet");
            args.ShouldContain("--force");
            args.ShouldContain("--skip-dependencies");
            args.ShouldContain("--header");
            args.ShouldContain("X-Custom");
            args.ShouldContain("--installer-type");
            args.ShouldContain("exe");
            args.ShouldContain("--custom");
            args.ShouldContain("--extra");
            args.ShouldContain("--locale");
            args.ShouldContain("en-GB");
            args.ShouldContain("--accept-package-agreements");
            args.ShouldContain("--accept-source-agreements");
            args.ShouldContain("--ignore-security-hash");
        }

        [Fact]
        public void WithNoOptions_ReturnsMinimalArgs()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage { Id = "Minimal.Package" };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildInstallArgs(package);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldBe(new[] { "install", "--id", "Minimal.Package" });
        }
    }

    public class BuildUninstallArgsEdgeCases : WinGetArgumentBuilderTests
    {
        [Fact]
        public void WithForceOnly_ReturnsForceFlag()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var options = new UninstallOptions
            {
                Id = "Test.Package",
                Force = true
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildUninstallArgs(options);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldContain("--force");
            args.ShouldNotContain("--silent");
            args.ShouldNotContain("--interactive");
        }

        [Fact]
        public void WithInteractiveAndSilent_ReturnsBothFlags()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var options = new UninstallOptions
            {
                Id = "Test.Package",
                Interactive = true,
                Silent = true
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildUninstallArgs(options);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldContain("--interactive");
            args.ShouldContain("--silent");
        }

        [Fact]
        public void WithMinimalOptions_ReturnsOnlyId()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var options = new UninstallOptions { Id = "Test.Package" };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var args = _builder.BuildUninstallArgs(options);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            args.ShouldBe(new[] { "uninstall", "--id", "Test.Package" });
        }
    }
}



