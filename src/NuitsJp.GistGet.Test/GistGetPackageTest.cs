using Shouldly;

namespace NuitsJp.GistGet.Test;

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
            result.ShouldBe("Sample App [Sample.Package]");
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

    public class ToDisplayString : GistGetPackageTests
    {
        [Fact]
        public void WithColorAndName_ReturnsCyanNameAndIdWithBrackets()
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
            var result = package.ToDisplayString(colorize: true);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe("\u001b[96mSample App\u001b[0m [\u001b[96mSample.Package\u001b[0m]");
        }

        [Fact]
        public void WithColorAndNoName_ReturnsCyanIdOnly()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var package = new GistGetPackage
            {
                Id = "Sample.Package"
            };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = package.ToDisplayString(colorize: true);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe("\u001b[96mSample.Package\u001b[0m");
        }
    }
}




