using Shouldly;

namespace GistGet.Test;

public class GistGetPackageTests
{
    public class ToStringOverride : GistGetPackageTests
    {
        [Fact]
        public void WhenNameExists_ReturnsNameAndId()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage
            {
                Id = "Sample.Package",
                Name = "Sample App"
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.ToString();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe("Sample App (Sample.Package)");
        }

        [Fact]
        public void WhenNameIsMissing_ReturnsIdOnly()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage
            {
                Id = "Sample.Package",
                Name = null
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.ToString();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe("Sample.Package");
        }
    }
}
