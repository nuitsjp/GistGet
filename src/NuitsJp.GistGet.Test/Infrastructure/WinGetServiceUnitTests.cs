using System.Diagnostics;
using Moq;
using NuitsJp.GistGet.Infrastructure;
using Shouldly;

namespace NuitsJp.GistGet.Test.Infrastructure;

public class WinGetServiceUnitTests
{
    protected readonly Mock<IProcessRunner> ProcessRunnerMock = new();
    protected readonly WinGetService Target;
    protected ProcessResult ListResult = new(0, "Example [Example.Package]\nVersion: 1.0", "");
    protected ProcessResult SourcesResult = new(0, "{\"id\":\"Example.Package\",\"source\":\"winget\"}", "");

    public WinGetServiceUnitTests()
    {
        ProcessRunnerMock.Setup(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p => p.ArgumentList[0] == "list")))
            .Returns(() => ListResult);
        ProcessRunnerMock.Setup(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p => p.ArgumentList[0] == "dscv3")))
            .Returns(() => SourcesResult);
        Target = new WinGetService(ProcessRunnerMock.Object);
    }

    public class GetAllInstalledPackages : WinGetServiceUnitTests
    {
        [Fact]
        public void LocalizedDetails_ReturnsFullPackageInformation()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, """
                (1/2) 日本語アプリ [Preview] [Example.VeryLongPackageIdentifier.WithoutTruncation]
                バージョン: 1.2.3
                発行元: Example publisher
                利用可能なアップグレード:
                  winget [2.0]
                (2/2) Microsoft OneNote - fr-fr [XPFFZHVGQWWLHB]
                Version : 16.0.1
                Éditeur : Microsoft Corporation
                """, "");
            SourcesResult = new ProcessResult(0, """
                {"id":"Example.VeryLongPackageIdentifier.WithoutTruncation","source":"winget"}
                {"id":"XPFFZHVGQWWLHB","source":"msstore"}
                """, "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = Target.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.Count.ShouldBe(2);
            packages[0].Name.ShouldBe("日本語アプリ [Preview]");
            packages[0].Id.AsPrimitive().ShouldBe("Example.VeryLongPackageIdentifier.WithoutTruncation");
            packages[0].Version.ShouldBe(new Version("1.2.3"));
            packages[0].UsableVersion.ShouldBe(new Version("2.0"));
            packages[0].Source.ShouldBe("winget");
            packages[1].Version.ShouldBe(new Version("16.0.1"));
            packages[1].Source.ShouldBe("msstore");
        }

        [Fact]
        public void CommandFails_ThrowsWithReasonAndExitCode()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(unchecked((int)0x800706BA), "", "RPC unavailable");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exception = Should.Throw<InvalidOperationException>(() => Target.GetAllInstalledPackages());

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exception.Message.ShouldContain("800706BA");
            exception.Message.ShouldContain("RPC unavailable");
        }

        [Fact]
        public void NoInstalledPackageExitCode_ReturnsEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(unchecked((int)0x8A150014), "No installed package found matching input criteria.", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = Target.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("not package details")]
        [InlineData("Example [Example.Package]")]
        [InlineData("(1/2) Example [Example.Package]\nVersion: 1.0")]
        public void SuccessfulMalformedOutput_Throws(string output)
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, output, "");

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            Should.Throw<InvalidOperationException>(() => Target.GetAllInstalledPackages());
        }

        [Fact]
        public void DuplicateInstalledIds_PrefersEntryWithAvailableUpdate()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, """
                (1/2) Office en-us [Microsoft.Office]
                Version: 16.0.1
                (2/2) Office ja-jp [Microsoft.Office]
                Version: 16.0.1
                Available Upgrades:
                  winget [16.0.2]
                """, "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = Target.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.ShouldHaveSingleItem().UsableVersion.ShouldBe(new Version("16.0.2"));
        }

        [Fact]
        public void SourceExportFails_ThrowsInsteadOfUsingIncompleteSources()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            SourcesResult = new ProcessResult(unchecked((int)0x80070005), "", "Access denied");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exception = Should.Throw<InvalidOperationException>(() => Target.GetAllInstalledPackages());

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exception.Message.ShouldContain("80070005");
            exception.Message.ShouldContain("Access denied");
        }

        [Fact]
        public void MalformedSourceExport_ThrowsInsteadOfReturningPartialList()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            SourcesResult = new ProcessResult(0, "{invalid json}", "");

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            Should.Throw<InvalidOperationException>(() => Target.GetAllInstalledPackages());
        }
    }

    public class FindById : WinGetServiceUnitTests
    {
        [Fact]
        public void StoreId_QueriesInstalledPackageByExactId()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var id = new PackageId("XPFFZHVGQWWLHB");
            ListResult = new ProcessResult(0, "OneNote [XPFFZHVGQWWLHB]\nVersion: 16.0.1", "");
            SourcesResult = new ProcessResult(0, "{\"id\":\"XPFFZHVGQWWLHB\",\"source\":\"msstore\"}", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var package = Target.FindById(id);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            package.ShouldNotBeNull();
            package.Id.ShouldBe(id);
            package.Source.ShouldBe("msstore");
            ProcessRunnerMock.Verify(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p =>
                p.ArgumentList[0] == "list" && p.ArgumentList.Contains("--details") &&
                p.ArgumentList.Contains("--id") && p.ArgumentList.Contains("XPFFZHVGQWWLHB") &&
                p.ArgumentList.Contains("--exact") && p.ArgumentList.Contains("--disable-interactivity") &&
                p.ArgumentList.Contains("--accept-source-agreements"))), Times.Once);
        }

        [Fact]
        public void MultipleInstalledVersions_ReturnsNewestInstalledVersion()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, """
                (1/2) .NET SDK old [Microsoft.DotNet.SDK.10]
                Version: 10.0.303
                Available Upgrades:
                  winget [10.0.404]
                (2/2) .NET SDK current [Microsoft.DotNet.SDK.10]
                Version: 10.0.404
                """, "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var package = Target.FindById(new PackageId("Microsoft.DotNet.SDK.10"));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            package.ShouldNotBeNull();
            package.Version.ShouldBe(new Version("10.0.404"));
        }

        [Fact]
        public void PackageNotInstalled_ReturnsNull()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(unchecked((int)0x8A150014), "No installed package found matching input criteria.", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var package = Target.FindById(new PackageId("Example.Package"));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            package.ShouldBeNull();
        }
    }

    public class GetPackagesWithUpdates : WinGetServiceUnitTests
    {
        [Fact]
        public void NoUpgradeHeadersWithSuccessfulExit_ReturnsEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, "No installed package found matching input criteria.", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = Target.GetPackagesWithUpdates();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.ShouldBeEmpty();
        }

        [Fact]
        public void DefaultQuery_UsesUpgradeFilterWithoutUnknownVersions()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, "Example [Example.Package]\nVersion: 1.0\nAvailable Upgrades:\n  winget [2.0]", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var packages = Target.GetPackagesWithUpdates();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            packages.ShouldHaveSingleItem().UsableVersion.ShouldBe(new Version("2.0"));
            ProcessRunnerMock.Verify(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p =>
                p.ArgumentList[0] == "list" && p.ArgumentList.Contains("--upgrade-available") &&
                !p.ArgumentList.Contains("--include-unknown"))), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitUnknownOption_IsForwardedToWinGet(bool includeUnknown)
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0, "Example [Example.Package]\nVersion: Unknown\nAvailable Upgrades:\n  winget [2.0]", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            Target.GetPackagesWithUpdates(includeUnknown);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ProcessRunnerMock.Verify(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p =>
                p.ArgumentList[0] == "list" && p.ArgumentList.Contains("--upgrade-available") &&
                p.ArgumentList.Contains("--include-unknown") == includeUnknown)), Times.Once);
        }
    }
    public class GetPinnedPackages : WinGetServiceUnitTests
    {
        [Fact]
        public void WideNamesAndMissingInstalledVersion_PreservePinIdsAndVersions()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ProcessRunnerMock.Setup(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p => p.ArgumentList[0] == "pin")))
                .Returns(new ProcessResult(0, """
                    Name          Id                     Version Source Pin type Pinned version
                    --------------------------------------------------------------------------
                    日本語ソフト  Example.Package        1.0     winget Gating   1.0.*
                    Store app     9NTRHF51WGGB                   msstore Blocking
                    """, ""));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var pins = Target.GetPinnedPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            pins.Count.ShouldBe(2);
            pins[0].Id.AsPrimitive().ShouldBe("Example.Package");
            pins[0].PinnedVersion.ShouldBe(new Version("1.0.*"));
            pins[1].Id.AsPrimitive().ShouldBe("9NTRHF51WGGB");
            pins[1].PinType.ShouldBe("Blocking");
            pins[1].PinnedVersion.ShouldBeNull();
        }

        [Fact]
        public void InstalledVersionContainsSpaces_ReturnsPin()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ProcessRunnerMock.Setup(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p => p.ArgumentList[0] == "pin")))
                .Returns(new ProcessResult(0, """
                    Name            Id                                    Version                      Source Pin type Pinned version
                    ---------------------------------------------------------------------------------------------------------------
                    Visual Studio   Microsoft.VisualStudio.2022.Enterprise 17.14.21 (November 2025)      winget Gating   17.*
                    """, ""));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var pins = Target.GetPinnedPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            pins.ShouldHaveSingleItem().Id.AsPrimitive().ShouldBe("Microsoft.VisualStudio.2022.Enterprise");
        }

        [Fact]
        public void CommandFails_ThrowsInsteadOfReturningNoPins()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ProcessRunnerMock.Setup(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p => p.ArgumentList[0] == "pin")))
                .Returns(new ProcessResult(17, "", "pinning index unavailable"));

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exception = Should.Throw<InvalidOperationException>(() => Target.GetPinnedPackages());

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exception.Message.ShouldContain("pinning index unavailable");
            exception.Message.ShouldContain("00000011");
        }
    }

}
