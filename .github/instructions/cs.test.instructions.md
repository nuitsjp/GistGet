---
applyTo: "**/*.Test.cs"
---

# Sample Instructions for C# Test Files

```csharp
namespace GistGet.Infrastructure.Com;

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
            Assert.NotNull(result);
            Assert.Equal(packageId, result.Id);
            Assert.NotEmpty(result.Name);
            Assert.NotNull(result.UsableVersion);
        }
    }
}

```