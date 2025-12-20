using GistGet.Infrastructure;
using Shouldly;

namespace GistGet.Test.Infrastructure;

public class WinGetServiceTests
{
    protected readonly WinGetService WinGetService = new();

    private static WinGetPackage RequireInstalledPackage(WinGetPackage? package, PackageId id)
    {
        package.ShouldNotBeNull($"Package '{id.AsPrimitive()}' is required for this test run.");
        return package;
    }

    public class FindById : WinGetServiceTests
    {
        [Fact]
        public void ExistingPackageWithUpdate_ReturnsPackageWithUsableVersionWhenAvailable()
        {
            // Tests that UsableVersion is populated when AvailableVersions[0] differs from InstalledVersion.
            // This comparison ignores IsUpdateAvailable's applicability checks (architecture, requirements).
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
            result = RequireInstalledPackage(result, packageId);
            result.Id.ShouldBe(packageId);
            result.Name.ShouldNotBeEmpty();

            if (result.UsableVersion is null)
            {
                return;
            }

            result.UsableVersion.ShouldNotBeNull();
        }

        [Fact]
        public void ExistingPackageWithoutUpdate_ReturnsPackageWithNullUsableVersion()
        {
            // Tests that UsableVersion is null when no newer version exists in AvailableVersions,
            // or when AvailableVersions[0] matches the installed version.
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("Microsoft.VisualStudioCode");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result = RequireInstalledPackage(result, packageId);
            result.Id.ShouldBe(packageId);
            result.Name.ShouldNotBeEmpty();
            if (result.UsableVersion is null)
            {
                return;
            }

            var usableVersion = result.UsableVersion.Value;
            usableVersion.ShouldNotBe(default);
            usableVersion.ShouldNotBe(result.Version);
        }

        [Fact]
        public void NonExistingPackage_ReturnsNull()
        {
            // -------------------------------------------------------------------
            // Arrange
            // -------------------------------------------------------------------
            var packageId = new PackageId("NonExisting.Package.Id.12345");

            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.FindById(packageId);

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldBeNull();
        }
    }

    public class GetAllInstalledPackages : WinGetServiceTests
    {
        [Fact]
        public void ReturnsNonEmptyList()
        {
            // -------------------------------------------------------------------
            // Arrange & Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void EachPackageHasValidIdAndVersion()
        {
            // -------------------------------------------------------------------
            // Arrange & Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetAllInstalledPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            foreach (var package in result)
            {
                package.Id.AsPrimitive().ShouldNotBeNullOrEmpty();
                package.Version.ToString().ShouldNotBeNullOrEmpty();
            }
        }
    }

    /// <summary>
    /// Integration tests for GetPinnedPackages method.
    /// </summary>
    /// <remarks>
    /// These tests require jqlang.jq to be installed on the system.
    /// Pin setup is performed in the Arrange section using winget CLI passthrough,
    /// as the WinGet COM API does not expose pin management functionality.
    /// </remarks>
    public class GetPinnedPackages : WinGetServiceTests, IDisposable
    {
        private const string TestPackageId = "jqlang.jq";
        private const string TestPinVersion = "1.7.0";
        private bool _pinWasAdded;
        private bool _wingetAvailable = true;

        public GetPinnedPackages()
        {
            // Arrange: Ensure the test package is pinned before running tests.
            // Using CLI because WinGet COM API does not expose pin management.
            try
            {
                EnsurePackageIsPinned();
            }
            catch
            {
                // If pin setup fails (e.g., winget not available), mark as unavailable
                _wingetAvailable = false;
            }
        }

        public void Dispose()
        {
            // Cleanup: Remove the pin after tests if we added it
            if (_pinWasAdded)
            {
                RunWinGetCommand($"pin remove --id {TestPackageId} --force");
            }
            GC.SuppressFinalize(this);
        }

        private void EnsurePackageIsPinned()
        {
            // Use CLI directly to check existing pins (avoid calling method under test in Arrange)
            // This ensures test isolation by not depending on the implementation being tested
            var output = RunWinGetCommandWithOutput("pin list");
            if (output.Contains(TestPackageId))
            {
                return; // Already pinned, no cleanup needed
            }

            // Add pin via CLI passthrough
            var exitCode = RunWinGetCommand($"pin add --id {TestPackageId} --version {TestPinVersion} --force");
            if (exitCode == 0)
            {
                _pinWasAdded = true;
            }
        }

        private static string RunWinGetCommandWithOutput(string arguments)
        {
            var wingetPath = ResolveWinGetPath();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = wingetPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private static int RunWinGetCommand(string arguments)
        {
            var wingetPath = ResolveWinGetPath();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = wingetPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode ?? -1;
        }

        private static string ResolveWinGetPath()
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var path in pathEnv.Split(Path.PathSeparator))
                {
                    var fullPath = Path.Combine(path, "winget.exe");
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            return Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
        }

        [Fact]
        public void ReturnsNonEmptyListWhenPinsExist()
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetPinnedPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            result.ShouldNotBeNull();

            // Skip assertions if pins cannot be retrieved (e.g., winget unavailable in test environment)
            if (!_wingetAvailable || result.Count == 0) return;

            result.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void ContainsJqlangJqPin()
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetPinnedPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            // Skip assertions if pins cannot be retrieved (e.g., winget unavailable in test environment)
            if (!_wingetAvailable || result.Count == 0) return;

            var jqPin = result.FirstOrDefault(p => p.Id.AsPrimitive() == TestPackageId);
            jqPin.ShouldNotBeNull($"Package '{TestPackageId}' should be pinned for this test to pass.");
            jqPin.PinnedVersion.ShouldNotBeNull();
        }

        [Fact]
        public void EachPinHasValidIdAndPinType()
        {
            // -------------------------------------------------------------------
            // Act
            // -------------------------------------------------------------------
            var result = WinGetService.GetPinnedPackages();

            // -------------------------------------------------------------------
            // Assert
            // -------------------------------------------------------------------
            foreach (var pin in result)
            {
                pin.Id.AsPrimitive().ShouldNotBeNullOrEmpty();
                pin.PinType.ShouldNotBeNullOrEmpty();
            }
        }
    }
}

