using GistGet.Infrastructure;
using Shouldly;

namespace GistGet.Test.Infrastructure;

public class WinGetArgumentBuilderTests
{
    private readonly WinGetArgumentBuilder _builder = new();

    public class BuildInstallArgs_FromOptions : WinGetArgumentBuilderTests
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

    public class BuildUpgradeArgs_FromOptions : WinGetArgumentBuilderTests
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
    }

    public class BuildUninstallArgs_FromOptions : WinGetArgumentBuilderTests
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

    public class BuildPinAddArgs_FromParams : WinGetArgumentBuilderTests
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
    }
}
