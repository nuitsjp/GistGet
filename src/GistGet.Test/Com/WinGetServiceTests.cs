using GistGet.Com;

namespace GistGet.Test.Com;

public class WinGetServiceTests
{
    private readonly WinGetService _sut = new();

    [Fact]
    public async Task FindByIdAsync_ExistingPackageWithUpdate_ReturnsPackageWithUsableVersion()
    {
        // Arrange
        var packageId = new PackageId("jqlang.jq");

        // Act
        var result = await _sut.FindByIdAsync(packageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(packageId, result.Id);
        Assert.NotEmpty(result.Name);
        Assert.NotNull(result.UsableVersion);
    }

    [Fact]
    public async Task FindByIdAsync_ExistingPackageWithoutUpdate_ReturnsPackageWithNullUsableVersion()
    {
        // Arrange
        var packageId = new PackageId("Microsoft.VisualStudioCode");

        // Act
        var result = await _sut.FindByIdAsync(packageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(packageId, result.Id);
        Assert.NotEmpty(result.Name);
        Assert.Null(result.UsableVersion);
    }

    [Fact]
    public async Task FindByIdAsync_NonExistingPackage_ReturnsNull()
    {
        // Arrange
        var packageId = new PackageId("NonExisting.Package.Id.12345");

        // Act
        var result = await _sut.FindByIdAsync(packageId);

        // Assert
        Assert.Null(result);
    }
}
