namespace GistGet.Infrastructure.Com;

public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();
    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackageWithUpdate_ReturnsPackageWithUsableVersion()
        {
            // Arrange
            var packageId = new PackageId("jqlang.jq");

            // Act
            var result = WinGetService.FindById(packageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(packageId, result.Id);
            Assert.NotEmpty(result.Name);
            Assert.NotNull(result.UsableVersion);
        }

        [Fact]
        public void ExistingPackageWithoutUpdate_ReturnsPackageWithNullUsableVersion()
        {
            // Arrange
            var packageId = new PackageId("Microsoft.VisualStudioCode");

            // Act
            var result = WinGetService.FindById(packageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(packageId, result.Id);
            Assert.NotEmpty(result.Name);
            Assert.Null(result.UsableVersion);
        }

        [Fact]
        public void NonExistingPackage_ReturnsNull()
        {
            // Arrange
            var packageId = new PackageId("NonExisting.Package.Id.12345");

            // Act
            var result = WinGetService.FindById(packageId);

            // Assert
            Assert.Null(result);
        }
    }
}
