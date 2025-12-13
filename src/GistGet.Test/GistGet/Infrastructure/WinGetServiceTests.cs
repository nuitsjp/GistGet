namespace GistGet.Infrastructure;

using GistGet;
using Shouldly;
using Xunit;

[Trait("Category", "Integration")]
public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();

    private static WinGetPackage RequireInstalledPackage(WinGetPackage? package, PackageId id)
    {
        package.ShouldNotBeNull($"Package '{id.AsPrimitive()}' is required for this test run.");
        return package!;
    }

    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackageWithUpdate_ReturnsPackageWithUsableVersionWhenAvailable()
        {
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
            result.UsableVersion.ShouldBeNull();
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
