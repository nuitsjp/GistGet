using System.Xml.Linq;
using Shouldly;

namespace GistGet.Test;

public class ProjectFileTests
{
    [Fact]
    public void Csproj_ShouldIncludeAllContentForSelfExtract()
    {
        // -------------------------------------------------------------------
        // Arrange
        // -------------------------------------------------------------------
        var projectPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "..", "..",
                "src", "GistGet", "GistGet.csproj"));

        // -------------------------------------------------------------------
        // Act
        // -------------------------------------------------------------------
        var document = XDocument.Load(projectPath);
        var propertyValue = document
            .Descendants("IncludeAllContentForSelfExtract")
            .Select(x => x.Value.Trim())
            .FirstOrDefault();

        // -------------------------------------------------------------------
        // Assert
        // -------------------------------------------------------------------
        propertyValue.ShouldNotBeNull();
        propertyValue.ShouldBe("true", StringCompareShould.IgnoreCase);
    }
}
