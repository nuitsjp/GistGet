using System.Collections;

namespace NuitsJp.GistGet.Models;

public class PackageCollection : IEnumerable<PackageDefinition>
{
    private readonly HashSet<PackageDefinition> _packages;

    public PackageCollection()
    {
        _packages = new HashSet<PackageDefinition>();
    }

    public int Count => _packages.Count;

    public void Add(PackageDefinition package)
    {
        if (package == null)
            throw new ArgumentNullException(nameof(package));

        _packages.Add(package);
    }

    public bool Remove(PackageDefinition package)
    {
        if (package == null)
            return false;

        return _packages.Remove(package);
    }

    public bool Contains(PackageDefinition package)
    {
        return _packages.Contains(package);
    }

    public PackageDefinition? FindById(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return null;

        return _packages.FirstOrDefault(p =>
            string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));
    }

    public List<PackageDefinition> ToSortedList()
    {
        var list = new List<PackageDefinition>(_packages);
        list.Sort();
        return list;
    }

    public void Clear()
    {
        _packages.Clear();
    }

    public IEnumerator<PackageDefinition> GetEnumerator()
    {
        return _packages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}