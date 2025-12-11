namespace GistGet.Infrastructure.Com;

using Shouldly;

public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();
    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackageWithUpdate_ReturnsPackageWithUsableVersion()
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
            result.ShouldNotBeNull();
            result.Id.ShouldBe(packageId);
            result.Name.ShouldNotBeEmpty();
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
            result.ShouldNotBeNull();
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
}
