using Shouldly;

namespace GistGet.Test;

public class SyncResultTests
{
    public class Failed : SyncResultTests
    {
        [Fact]
        public void ShouldBeDictionaryWithExitCodes()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var result = new SyncResult();
            var package1 = new GistGetPackage { Id = "Test.Package1", Name = "Test Package 1" };
            var package2 = new GistGetPackage { Id = "Test.Package2", Name = "Test Package 2" };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            result.Failed[package1] = -1978335189;
            result.Failed[package2] = -1;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Failed.Count.ShouldBe(2);
            result.Failed[package1].ShouldBe(-1978335189);
            result.Failed[package2].ShouldBe(-1);
        }
    }

    public class Success : SyncResultTests
    {
        [Fact]
        public void ShouldBeTrueWhenNoFailures()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var result = new SyncResult();

            // -------------------------------------------------------------------
            // Act & Assert
            // -------------------------------------------------------------------
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public void ShouldBeFalseWhenHasFailures()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var result = new SyncResult();
            var package = new GistGetPackage { Id = "Test.Package", Name = "Test Package" };

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            result.Failed[package] = -1;

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.Success.ShouldBeFalse();
        }
    }
}
