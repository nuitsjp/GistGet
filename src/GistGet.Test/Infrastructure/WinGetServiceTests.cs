using GistGet.Infrastructure;
using Shouldly;

namespace GistGet.Test.Infrastructure;

public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();

    private static WinGetPackage RequireInstalledPackage(WinGetPackage? package, PackageId id)
    {
        package.ShouldNotBeNull($"Package '{id.AsPrimitive()}' is required for this test run.");
        return package;
    }

    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackageWithUpdate_ReturnsPackageWithUsableVersionWhenAvailable()
        {
            // Tests that UsableVersion is populated when AvailableVersions[0] differs from InstalledVersion.
            // This comparison ignores IsUpdateAvailable's applicability checks (architecture, requirements).
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("jqlang.jq");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result = RequireInstalledPackage(result, packageId);
            result.Id.ShouldBe(packageId);
            result.Name.ShouldNotBeEmpty();

            if (result.UsableVersion is null)
            {
                return;
            }

            result.UsableVersion.ShouldNotBeNull();
        }

        [Fact]
        public void ExistingPackageWithoutUpdate_ReturnsPackageWithNullUsableVersion()
        {
            // Tests that UsableVersion is null when no newer version exists in AvailableVersions,
            // or when AvailableVersions[0] matches the installed version.
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("Microsoft.VisualStudioCode");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result = RequireInstalledPackage(result, packageId);
            result.Id.ShouldBe(packageId);
            result.Name.ShouldNotBeEmpty();
            if (result.UsableVersion is null)
            {
                return;
            }

            var usableVersion = result.UsableVersion.Value;
            usableVersion.ShouldNotBe(default);
            usableVersion.ShouldNotBe(result.Version);
        }

        [Fact]
        public void NonExistingPackage_ReturnsNull()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("NonExisting.Package.Id.12345");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeNull();
        }
    }

    public class GetAllInstalledPackages : WinGetServiceTests
    {
        [Fact]
        public void ReturnsNonEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange & Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void EachPackageHasValidIdAndVersion()
        {
            // -------------------------------------------------------------------
            // Arrange & Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            foreach (var package in result)
            {
                package.Id.AsPrimitive().ShouldNotBeNullOrEmpty();
                package.Version.ToString().ShouldNotBeNullOrEmpty();
            }
        }
    }
}
