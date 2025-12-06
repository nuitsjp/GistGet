using GistGet.Com;

namespace GistGet.Test.Com;

public class WinGetServiceTests
{
    private readonly WinGetService _sut = new();
    [Fact]
    public void FindById_ExistingPackageWithUpdate_ReturnsPackageWithUsableVersion()
    {
        // Arrange
        var packageId = new PackageId("jqlang.jq");

        // Act
        var result = _sut.FindById(packageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(packageId, result.Id);
        Assert.NotEmpty(result.Name);
        Assert.NotNull(result.UsableVersion);
    }

    [Fact]
    public void FindById_ExistingPackageWithoutUpdate_ReturnsPackageWithNullUsableVersion()
    {
        // Arrange
        var packageId = new PackageId("Microsoft.VisualStudioCode");

        // Act
        var result = _sut.FindById(packageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(packageId, result.Id);
        Assert.NotEmpty(result.Name);
        Assert.Null(result.UsableVersion);
    }

    [Fact]
    public void FindById_NonExistingPackage_ReturnsNull()
    {
        // Arrange
        var packageId = new PackageId("NonExisting.Package.Id.12345");

        // Act
        var result = _sut.FindById(packageId);

        // Assert
        Assert.Null(result);
    }
}
