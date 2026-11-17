using Microsoft.Management.Deployment;
using Moq;
using NuitsJp.GistGet.Infrastructure;
using Shouldly;

namespace NuitsJp.GistGet.Test.Infrastructure;

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
        public void WhenConnectThrows_PreservesFailure()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var connector = new Mock<IPackageCatalogConnector>();
            var failure = (System.Runtime.InteropServices.COMException)System.Runtime.InteropServices.Marshal.GetExceptionForHR(unchecked((int)0x80040154))!;
            connector.Setup(x => x.Connect(It.IsAny<CompositeSearchBehavior>())).Throws(failure);
            var target = new WinGetService(connector.Object);

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var exception = Should.Throw<System.Runtime.InteropServices.COMException>(() => target.FindById(new PackageId("Test.Package")));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exception.ShouldBe(failure);
        }

        [Fact]
        public void WhenConnectFails_ThrowsInsteadOfTreatingPackageAsMissing()
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
            var exception = Should.Throw<InvalidOperationException>(() => target.FindById(new PackageId("Test.Package")));

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exception.Message.ShouldContain("catalog");
            connector.Verify(x => x.Connect(It.IsAny<CompositeSearchBehavior>()), Times.Once);
        }
    }

    public class GetAllInstalledPackagesWithMock : WinGetServiceUnitTests
    {
        [Fact]
        public void WhenConnectThrows_PreservesFailure()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var connector = new Mock<IPackageCatalogConnector>();
            var failure = (System.Runtime.InteropServices.COMException)System.Runtime.InteropServices.Marshal.GetExceptionForHR(unchecked((int)0x80040154))!;
            connector.Setup(x => x.Connect(It.IsAny<CompositeSearchBehavior>())).Throws(failure);
            var target = new WinGetService(connector.Object);

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            Should.Throw<System.Runtime.InteropServices.COMException>(() => target.GetAllInstalledPackages()).ShouldBe(failure);
        }

        [Fact]
        public void WhenConnectFails_ThrowsInsteadOfReturningEmptyList()
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
            var exception = Should.Throw<InvalidOperationException>(() => target.GetAllInstalledPackages());

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            exception.Message.ShouldContain("catalog");
        }
    }
}




