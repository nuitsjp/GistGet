using Shouldly;
using Xunit;

namespace GistGet;

public class ConstantsTests
{
    public class DefaultValues
    {
        [Fact]
        public void DefaultGistFileName_ShouldBeGistGetYaml()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = Constants.DefaultGistFileName;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBe("gistget.yaml");
        }
    }
}
