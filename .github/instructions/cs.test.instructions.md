---
applyTo: "**/*.Test.cs"
---

# C# Test File Coding Guidelines

## General Rules

- Use **xUnit** as the test framework
- Use **Shouldly** as the assertion library
- Use **Moq** as the mocking library

## File and Class Structure

- Test file names should follow `{TargetClassName}.Test.cs`
- Test class names should follow `{TargetClassName}Tests`
- Create nested classes for each method under test, inheriting from the test class
- Define shared setup in `protected` fields of the parent class

## Test Method Naming Convention

- Naming pattern: `{Scenario}_{ExpectedResult}`
- Example: `ExistingPackageWithUpdate_ReturnsPackageWithUsableVersion`

## Test Method Structure (AAA Pattern)

Each test method should consist of the following 3 sections, explicitly separated by comments:

1. **Arrange** - Set up the test preconditions
2. **Act** - Execute the operation under test
3. **Assert** - Verify the results

Use the following comment format for section separators:

```csharp
// -------------------------------------------------------------------
// Arrange
// -------------------------------------------------------------------
```

## Sample Code

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
            result.ShouldNotBeNull();
            result.Id.ShouldBe(packageId);
            result.Name.ShouldNotBeEmpty();
            result.UsableVersion.ShouldNotBeNull();
        }
    }
}
```

## Additional Guidelines

- Each test method should test only one behavior
- Tests must be independent and executable without relying on other tests
- Avoid magic numbers; use meaningful variable names
- Isolate external dependencies (APIs, databases, etc.) using mocks
