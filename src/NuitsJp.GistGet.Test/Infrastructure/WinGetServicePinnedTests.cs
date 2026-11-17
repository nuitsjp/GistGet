using System.Diagnostics;
using Moq;
using NuitsJp.GistGet.Infrastructure;

namespace NuitsJp.GistGet.Test.Infrastructure;

public class WinGetServicePinnedTests : WinGetServiceUnitTests
{
    public class GetPackagesWithUpdatesIncludingPinned : WinGetServicePinnedTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void InclusionOptions_AreForwardedToWinGet(bool includeUnknown, bool includePinned)
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            ListResult = new ProcessResult(0,
                "Example [Example.Package]\nVersion: 1.0\nAvailable Upgrades:\n  winget [2.0]", "");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            Target.GetPackagesWithUpdates(includeUnknown, includePinned);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            ProcessRunnerMock.Verify(x => x.RunAndCapture(It.Is<ProcessStartInfo>(p =>
                p.ArgumentList[0] == "list" &&
                p.ArgumentList.Contains("--upgrade-available") &&
                p.ArgumentList.Contains("--include-unknown") == includeUnknown &&
                p.ArgumentList.Contains("--include-pinned") == includePinned)), Times.Once);
        }
    }
}
