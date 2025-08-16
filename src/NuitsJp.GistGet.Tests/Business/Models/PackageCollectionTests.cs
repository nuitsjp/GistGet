using NuitsJp.GistGet.Models;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Business.Models;

public class PackageCollectionTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyCollection()
    {
        // Act
        var collection = new PackageCollection();

        // Assert
        collection.Count.ShouldBe(0);
        collection.ShouldBeEmpty();
    }

    [Fact]
    public void Add_ShouldAddPackageToCollection()
    {
        // Arrange
        var collection = new PackageCollection();
        var package = new PackageDefinition("AkelPad.AkelPad");

        // Act
        collection.Add(package);

        // Assert
        collection.Count.ShouldBe(1);
        collection.Contains(package).ShouldBeTrue();
    }

    [Fact]
    public void Add_ShouldNotAddDuplicatePackages()
    {
        // Arrange
        var collection = new PackageCollection();
        var package1 = new PackageDefinition("AkelPad.AkelPad");
        var package2 = new PackageDefinition("AkelPad.AkelPad"); // Same ID

        // Act
        collection.Add(package1);
        collection.Add(package2);

        // Assert
        collection.Count.ShouldBe(1);
        collection.Contains(package1).ShouldBeTrue();
    }

    [Fact]
    public void Remove_ShouldRemovePackageFromCollection()
    {
        // Arrange
        var collection = new PackageCollection();
        var package = new PackageDefinition("AkelPad.AkelPad");
        collection.Add(package);

        // Act
        var removed = collection.Remove(package);

        // Assert
        removed.ShouldBeTrue();
        collection.Count.ShouldBe(0);
        collection.Contains(package).ShouldBeFalse();
    }

    [Fact]
    public void Remove_WhenPackageNotExists_ShouldReturnFalse()
    {
        // Arrange
        var collection = new PackageCollection();
        var package = new PackageDefinition("AkelPad.AkelPad");

        // Act
        var removed = collection.Remove(package);

        // Assert
        removed.ShouldBeFalse();
        collection.Count.ShouldBe(0);
    }

    [Fact]
    public void FindById_ShouldReturnMatchingPackage()
    {
        // Arrange
        var collection = new PackageCollection();
        var package = new PackageDefinition("AkelPad.AkelPad");
        collection.Add(package);

        // Act
        var found = collection.FindById("AkelPad.AkelPad");

        // Assert
        found.ShouldNotBeNull();
        found.Id.ShouldBe("AkelPad.AkelPad");
    }

    [Fact]
    public void FindById_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var collection = new PackageCollection();

        // Act
        var found = collection.FindById("NonExistent.Package");

        // Assert
        found.ShouldBeNull();
    }

    [Fact]
    public void ToSortedList_ShouldReturnPackagesSortedById()
    {
        // Arrange
        var collection = new PackageCollection();
        var package1 = new PackageDefinition("Zoom.Zoom");
        var package2 = new PackageDefinition("AkelPad.AkelPad");
        var package3 = new PackageDefinition("Microsoft.VisualStudioCode");

        collection.Add(package1);
        collection.Add(package2);
        collection.Add(package3);

        // Act
        var sortedList = collection.ToSortedList();

        // Assert
        sortedList.Count.ShouldBe(3);
        sortedList[0].Id.ShouldBe("AkelPad.AkelPad");
        sortedList[1].Id.ShouldBe("Microsoft.VisualStudioCode");
        sortedList[2].Id.ShouldBe("Zoom.Zoom");
    }

    [Fact]
    public void Clear_ShouldRemoveAllPackages()
    {
        // Arrange
        var collection = new PackageCollection();
        collection.Add(new PackageDefinition("Package1"));
        collection.Add(new PackageDefinition("Package2"));

        // Act
        collection.Clear();

        // Assert
        collection.Count.ShouldBe(0);
        collection.ShouldBeEmpty();
    }

    [Fact]
    public void Enumeration_ShouldWorkCorrectly()
    {
        // Arrange
        var collection = new PackageCollection();
        var package1 = new PackageDefinition("Package1");
        var package2 = new PackageDefinition("Package2");
        collection.Add(package1);
        collection.Add(package2);

        // Act
        var packages = collection.ToList();

        // Assert
        packages.Count.ShouldBe(2);
        packages.ShouldContain(package1);
        packages.ShouldContain(package2);
    }
}