using GistGet.Infrastructure;
using Microsoft.Management.Deployment;
using Moq;
using Shouldly;

namespace GistGet.Test.Infrastructure;

/// <summary>
/// Unit tests for WinGetService using mocked IPackageCatalogConnector.
/// These tests cover error paths and edge cases that are difficult to test
/// with the actual WinGet COM APIs.
/// </summary>
public class WinGetServiceUnitTests
{
    public class FindByIdWithMock : WinGetServiceUnitTests
    {
        [Fact]
        public void WhenConnectFails_ReturnsNull()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var connector = new Mock<IPackageCatalogConnector>();
            connector
                .Setup(x => x.Connect(It.IsAny<CompositeSearchBehavior>()))
                .Returns((PackageCatalog?)null);

            var target = new WinGetService(connector.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = target.FindById(new PackageId("Test.Package"));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeNull();
        }
    }

    public class GetAllInstalledPackagesWithMock : WinGetServiceUnitTests
    {
        [Fact]
        public void WhenConnectFails_ReturnsEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var connector = new Mock<IPackageCatalogConnector>();
            connector
                .Setup(x => x.Connect(It.IsAny<CompositeSearchBehavior>()))
                .Returns((PackageCatalog?)null);

            var target = new WinGetService(connector.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = target.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }
}
