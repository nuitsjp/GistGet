using Shouldly;

namespace GistGet.Test;

// ReSharper disable once ClassNeverInstantiated.Global
public class GistGetPackageSerializerTests
{
    public class Serialize
    {
        [Fact]
        public void WithSinglePackage_ReturnsYamlWithIdAsKey()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new() { Id = "Test.Package", Silent = true }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            yaml.ShouldContain("Test.Package:");
            yaml.ShouldContain("silent: true");
        }

        [Fact]
        public void WithMultiplePackages_ReturnsAllPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new() { Id = "Package.A", Scope = "user" },
                new() { Id = "Package.B", Scope = "machine" }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            yaml.ShouldContain("Package.A:");
            yaml.ShouldContain("Package.B:");
        }

        [Fact]
        public void WithAllProperties_PreservesAllValues()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new()
                {
                    Id = "Full.Package",
                    Version = "1.0.0",
                    Pin = "1.0.0",
                    PinType = "blocking",
                    Custom = "/CUSTOM",
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
                    Header = "X-Custom",
                    InstallerType = "msi",
                    Log = @"C:\log.txt",
                    Override = "/SILENT",
                    Interactive = false,
                    Silent = true
                }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            yaml.ShouldContain("version: 1.0.0");
            yaml.ShouldContain("pin: 1.0.0");
            yaml.ShouldContain("pinType: blocking");
            yaml.ShouldContain("custom: /CUSTOM");
            yaml.ShouldContain("scope: machine");
            yaml.ShouldContain("architecture: x64");
            yaml.ShouldContain("allowHashMismatch: true");
            yaml.ShouldContain("force: true");
            yaml.ShouldContain("acceptPackageAgreements: true");
            yaml.ShouldContain("acceptSourceAgreements: true");
            yaml.ShouldContain("skipDependencies: true");
            yaml.ShouldContain("installerType: msi");
            yaml.ShouldContain("silent: true");
        }

        [Fact]
        public void WithEmptyId_ThrowsArgumentException()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new() { Id = "" }
            };

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            Should.Throw<ArgumentException>(() => GistGetPackageSerializer.Serialize(packages));
        }

        [Fact]
        public void WithDefaultValues_OmitsDefaults()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new() { Id = "Minimal.Package" }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            yaml.ShouldNotContain("uninstall:");
            yaml.ShouldNotContain("silent:");
            yaml.ShouldNotContain("force:");
        }

        [Fact]
        public void WithEmptyPackage_WritesNullValueInsteadOfEmptyMapping()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new() { Id = "Empty.Package" }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(packages);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            yaml.ShouldNotContain("{}");
            yaml.Trim().ShouldBe("Empty.Package:");
        }

        [Fact]
        public void WithMultiplePackages_SerializesKeysInAscendingOrder()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packages = new List<GistGetPackage>
            {
                new() { Id = "Zeta.Package" },
                new() { Id = "Alpha.Package" }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(packages);
            var lines = yaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var keys = lines.Select(line => line.Split(':')[0]).ToList();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            keys.ShouldBe(new[] { "Alpha.Package", "Zeta.Package" });
        }
    }

    public class Deserialize
    {
        [Fact]
        public void WithValidYaml_ReturnsPackages()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var yaml = """
                       Test.Package:
                         silent: true
                         scope: user
                       """;

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = GistGetPackageSerializer.Deserialize(yaml);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.Count.ShouldBe(1);
            packages[0].Id.ShouldBe("Test.Package");
            packages[0].Silent.ShouldBeTrue();
            packages[0].Scope.ShouldBe("user");
        }

        [Fact]
        public void WithMultiplePackages_ReturnsAllSortedById()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var yaml = """
                       Zebra.Package:
                         scope: user
                       Alpha.Package:
                         scope: machine
                       """;

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = GistGetPackageSerializer.Deserialize(yaml);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.Count.ShouldBe(2);
            packages[0].Id.ShouldBe("Alpha.Package");
            packages[1].Id.ShouldBe("Zebra.Package");
        }

        [Fact]
        public void WithAllProperties_PreservesAllValues()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var yaml = """
                       Full.Package:
                         version: "2.0.0"
                         pin: "2.0.0"
                         pinType: gating
                         custom: /ARG
                         uninstall: true
                         scope: user
                         architecture: arm64
                         location: D:\Apps
                         locale: en-US
                         allowHashMismatch: true
                         force: true
                         acceptPackageAgreements: true
                         acceptSourceAgreements: true
                         skipDependencies: true
                         header: X-Test
                         installerType: exe
                         log: D:\log.txt
                         override: /VERYSILENT
                         interactive: true
                         silent: false
                       """;

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = GistGetPackageSerializer.Deserialize(yaml);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            var pkg = packages[0];
            pkg.Id.ShouldBe("Full.Package");
            pkg.Version.ShouldBe("2.0.0");
            pkg.Pin.ShouldBe("2.0.0");
            pkg.PinType.ShouldBe("gating");
            pkg.Custom.ShouldBe("/ARG");
            pkg.Uninstall.ShouldBeTrue();
            pkg.Scope.ShouldBe("user");
            pkg.Architecture.ShouldBe("arm64");
            pkg.Location.ShouldBe(@"D:\Apps");
            pkg.Locale.ShouldBe("en-US");
            pkg.AllowHashMismatch.ShouldBeTrue();
            pkg.Force.ShouldBeTrue();
            pkg.AcceptPackageAgreements.ShouldBeTrue();
            pkg.AcceptSourceAgreements.ShouldBeTrue();
            pkg.SkipDependencies.ShouldBeTrue();
            pkg.Header.ShouldBe("X-Test");
            pkg.InstallerType.ShouldBe("exe");
            pkg.Log.ShouldBe(@"D:\log.txt");
            pkg.Override.ShouldBe("/VERYSILENT");
            pkg.Interactive.ShouldBeTrue();
            pkg.Silent.ShouldBeFalse();
        }

        [Fact]
        public void WithEmptyPackageValue_CreatesPackageWithId()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var yaml = """
                       Empty.Package:
                       """;

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = GistGetPackageSerializer.Deserialize(yaml);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.Count.ShouldBe(1);
            packages[0].Id.ShouldBe("Empty.Package");
        }
    }

    public class RoundTrip
    {
        [Fact]
        public void SerializeAndDeserialize_PreservesAllData()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var original = new List<GistGetPackage>
            {
                new()
                {
                    Id = "RoundTrip.Package",
                    Version = "3.0.0",
                    Pin = "3.0.0",
                    PinType = "blocking",
                    Scope = "machine",
                    Silent = true,
                    Force = true
                }
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var yaml = GistGetPackageSerializer.Serialize(original);
            var result = GistGetPackageSerializer.Deserialize(yaml);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Count.ShouldBe(1);
            var pkg = result[0];
            pkg.Id.ShouldBe("RoundTrip.Package");
            pkg.Version.ShouldBe("3.0.0");
            pkg.Pin.ShouldBe("3.0.0");
            pkg.PinType.ShouldBe("blocking");
            pkg.Scope.ShouldBe("machine");
            pkg.Silent.ShouldBeTrue();
            pkg.Force.ShouldBeTrue();
        }
    }
}
