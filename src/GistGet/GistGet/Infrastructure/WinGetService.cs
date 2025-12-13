using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure;

public class WinGetService : IWinGetService
{
    public WinGetPackage? FindById(PackageId id)
    {
        var packageManager = new PackageManager();

        // インストール済みパッケージカタログと winget ソースを合成して検索
        var createCompositePackageCatalogOptions = new CreateCompositePackageCatalogOptions();

        var catalogs = packageManager.GetPackageCatalogs();
        // WinGet COM API との互換性問題を回避するため、インデックスベースでアクセス
        for (var i = 0; i < catalogs.Count; i++)
        {
            var catalogRef = catalogs[i];
            if (!catalogRef.Info.Explicit)
            {
                createCompositePackageCatalogOptions.Catalogs.Add(catalogRef);
            }
        }
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.AllCatalogs;

        var compositeRef = packageManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);
        var connectResult = compositeRef.Connect();

        if (connectResult.Status != ConnectResultStatus.Ok)
        {
            return null;
        }

        var catalog = connectResult.PackageCatalog;

        // ID による完全一致検索
        var findPackagesOptions = new FindPackagesOptions();
        findPackagesOptions.Selectors.Add(new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.EqualsCaseInsensitive,
            Value = id.AsPrimitive()
        });

        var findResult = catalog.FindPackages(findPackagesOptions);

        if (findResult.Status != FindPackagesResultStatus.Ok || findResult.Matches.Count == 0)
        {
            return null;
        }

        var catalogPackage = findResult.Matches[0].CatalogPackage;

        // インストールされていないパッケージは対象外
        if (catalogPackage.InstalledVersion == null)
        {
            return null;
        }

        var installedVersion = catalogPackage.InstalledVersion;

        // 更新可能なバージョンを取得
        // IsUpdateAvailable が正しく動作しない場合があるため、
        // AvailableVersions と InstalledVersion を比較して判断
        Version? usableVersion = null;
        if (catalogPackage.AvailableVersions.Count > 0)
        {
            var latestAvailableVersion = catalogPackage.AvailableVersions[0].Version;
            if (latestAvailableVersion != installedVersion.Version)
            {
                usableVersion = new Version(latestAvailableVersion);
            }
        }

        return new WinGetPackage(
            Name: catalogPackage.Name,
            Id: new PackageId(catalogPackage.Id),
            Version: new Version(installedVersion.Version),
            UsableVersion: usableVersion
        );
    }

    /// <summary>
    /// ローカルにインストールされている全パッケージを取得します。
    /// </summary>
    /// <returns>インストール済みパッケージの一覧</returns>
    public IReadOnlyList<WinGetPackage> GetAllInstalledPackages()
    {
        var packages = new List<WinGetPackage>();
        var packageManager = new PackageManager();

        // インストール済みパッケージカタログと winget ソースを合成して検索
        var createCompositePackageCatalogOptions = new CreateCompositePackageCatalogOptions();

        var catalogs = packageManager.GetPackageCatalogs();
        for (var i = 0; i < catalogs.Count; i++)
        {
            var catalogRef = catalogs[i];
            if (!catalogRef.Info.Explicit)
            {
                createCompositePackageCatalogOptions.Catalogs.Add(catalogRef);
            }
        }
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs;

        var compositeRef = packageManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);
        var connectResult = compositeRef.Connect();

        if (connectResult.Status != ConnectResultStatus.Ok)
        {
            return packages;
        }

        var catalog = connectResult.PackageCatalog;

        // すべてのパッケージを取得
        var findPackagesOptions = new FindPackagesOptions();
        var findResult = catalog.FindPackages(findPackagesOptions);

        if (findResult.Status != FindPackagesResultStatus.Ok)
        {
            return packages;
        }

        for (var i = 0; i < findResult.Matches.Count; i++)
        {
            var match = findResult.Matches[i];
            var catalogPackage = match.CatalogPackage;

            if (catalogPackage.InstalledVersion == null)
            {
                continue;
            }

            var installedVersion = catalogPackage.InstalledVersion;

            Version? usableVersion = null;
            if (catalogPackage.AvailableVersions.Count > 0)
            {
                var latestAvailableVersion = catalogPackage.AvailableVersions[0].Version;
                if (latestAvailableVersion != installedVersion.Version)
                {
                    usableVersion = new Version(latestAvailableVersion);
                }
            }

            packages.Add(new WinGetPackage(
                Name: catalogPackage.Name,
                Id: new PackageId(catalogPackage.Id),
                Version: new Version(installedVersion.Version),
                UsableVersion: usableVersion
            ));
        }

        return packages;
    }
}
