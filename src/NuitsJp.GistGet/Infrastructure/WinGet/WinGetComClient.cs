using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Deployment;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Infrastructure.WinGet;

/// <summary>
/// WinGet COM APIクライアント実装（アーキテクチャ改善版）
/// </summary>
public class WinGetComClient(ILogger<WinGetComClient> logger, IWinGetPassthroughClient passthroughClient) : IWinGetClient
{

    private bool _isInitialized;
    private PackageManager? _packageManager;

    public Task InitializeAsync()
    {
        if (_isInitialized) return Task.CompletedTask;

        try
        {
            logger.LogDebug("Initializing WinGet COM API");

            // 公式サンプルに合わせた簡単な初期化
            _packageManager = new PackageManager();

            if (_packageManager == null)
                throw new InvalidOperationException("PackageManager instance is null after creation");

            _isInitialized = true;
            logger.LogInformation("WinGet COM API initialized successfully");
            return Task.CompletedTask;
        }
        catch (COMException comEx)
        {
            var errorMessage = GetComErrorMessage(comEx.HResult);
            logger.LogError(comEx, "COM API initialization failed with HRESULT: 0x{HRESULT:X8} ({ErrorMessage})",
                comEx.HResult, errorMessage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize COM API: {ErrorMessage}, Exception type: {ExceptionType}",
                ex.Message, ex.GetType().FullName);
            throw;
        }
    }

    public async Task<int> InstallPackageAsync(string[] args)
    {
        if (!_isInitialized) return 1;
        var packageId = GetPackageId(args);
        if (packageId == null) return 1;

        try
        {
            logger.LogInformation("Installing package: {PackageId} via COM API", packageId);

            // 実際のCOM API呼び出し（公式サンプルに基づく修正）
            var catalogRef = _packageManager!.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
            var connectResult = await catalogRef.ConnectAsync();

            if (connectResult.Status != ConnectResultStatus.Ok)
            {
                logger.LogError("Failed to connect to winget catalog: {Status}", connectResult.Status);
                return await FallbackToWingetExe(args, packageId, "install");
            }

            // パッケージ検索（まずはIDで完全一致を試行）
            var findOptions = new FindPackagesOptions();
            var filter = new PackageMatchFilter
            {
                Field = PackageMatchField.Id,
                Option = PackageFieldMatchOption.Equals,
                Value = packageId
            };
            findOptions.Filters.Add(filter);

            var findResult = await connectResult.PackageCatalog.FindPackagesAsync(findOptions);

            // IDで見つからない場合は名前で検索
            if (findResult.Matches.Count == 0)
            {
                logger.LogWarning("Package not found by ID, trying name search: {PackageId}", packageId);
                findOptions.Filters.Clear();
                var nameFilter = new PackageMatchFilter
                {
                    Field = PackageMatchField.Name,
                    Option = PackageFieldMatchOption.ContainsCaseInsensitive,
                    Value = packageId
                };
                findOptions.Filters.Add(nameFilter);
                findResult = await connectResult.PackageCatalog.FindPackagesAsync(findOptions);
            }

            if (findResult.Matches.Count == 0)
            {
                logger.LogError("Package not found by ID or name: {PackageId}", packageId);
                return await FallbackToWingetExe(args, packageId, "install");
            }

            var package = findResult.Matches[0].CatalogPackage;
            logger.LogInformation("Found package: {PackageName} [{PackageId}] {Version}", package.Name, package.Id,
                package.DefaultInstallVersion?.Version ?? "unknown");

            // インストール実行（直接作成を試す）
            var installOptions = new InstallOptions
            {
                PackageInstallScope = PackageInstallScope.Any,
                PackageInstallMode = PackageInstallMode.Silent
            };
            var installResult = await _packageManager.InstallPackageAsync(package, installOptions);

            if (installResult.Status == InstallResultStatus.Ok)
            {
                logger.LogInformation("Successfully installed package: {PackageId}", packageId);
                return 0;
            }

            if (installResult.ExtendedErrorCode != null)
                logger.LogError("Installation failed: {Status}, Extended error: 0x{ExtendedError:X8}",
                    installResult.Status, installResult.ExtendedErrorCode);
            else
                logger.LogError("Installation failed: {Status}", installResult.Status);
            return await FallbackToWingetExe(args, packageId, "install");
        }
        catch (COMException comEx)
        {
            logger.LogWarning(comEx, "COM API call failed, falling back to winget.exe: 0x{HRESULT:X8}", comEx.HResult);
            return await FallbackToWingetExe(args, packageId, "install");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "COM API call failed, falling back to winget.exe");
            return await FallbackToWingetExe(args, packageId, "install");
        }
    }

    public async Task<int> UninstallPackageAsync(string[] args)
    {
        if (!_isInitialized) return 1;
        var packageId = GetPackageId(args);
        if (packageId == null) return 1;

        try
        {
            logger.LogInformation("Uninstalling package: {PackageId} via winget.exe (COM API uninstall not available)",
                packageId);

            // インストール済みパッケージから実際のパッケージIDを特定
            var installedPackages = await GetInstalledPackagesAsync();
            var actualPackage = installedPackages.FirstOrDefault(pkg =>
                pkg.Id.Contains(packageId, StringComparison.OrdinalIgnoreCase));

            var actualPackageId = actualPackage?.Id ?? packageId;

            if (actualPackage != null)
                logger.LogInformation("Found installed package: {ActualPackageId} for query: {QueryPackageId}",
                    actualPackageId, packageId);
            else
                logger.LogWarning("Package not found in installed list, using original ID: {PackageId}", packageId);

            // 実際のパッケージIDでwinget.exeを呼び出し
            string[] uninstallArgs = ["uninstall", actualPackageId, "--accept-source-agreements", "--disable-interactivity"];
            return await passthroughClient.ExecuteAsync(uninstallArgs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uninstalling package: {PackageId}, Error: {ErrorMessage}", packageId,
                ex.Message);
            return 1;
        }
    }

    public Task<int> UpgradePackageAsync(string[] args)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// インストール済みパッケージ一覧を取得（内部用）
    /// </summary>
    public async Task<List<PackageDefinition>> GetInstalledPackagesAsync()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("COM API not initialized");

        var packages = new List<PackageDefinition>();

        try
        {
            logger.LogDebug("Getting installed packages via COM API");

            // インストール済みパッケージカタログを取得
            var installedCatalogRef = _packageManager!.GetLocalPackageCatalog(LocalPackageCatalog.InstalledPackages);
            var connectResult = await installedCatalogRef.ConnectAsync();

            if (connectResult.Status != ConnectResultStatus.Ok)
            {
                logger.LogError("Failed to connect to installed packages catalog: {Status}", connectResult.Status);
                return packages;
            }

            // 全パッケージを検索
            var findOptions = new FindPackagesOptions();
            var findResult = await connectResult.PackageCatalog.FindPackagesAsync(findOptions);

            // 結果を処理
            for (var i = 0; i < findResult.Matches.Count; i++)
                try
                {
                    var match = findResult.Matches[i];
                    var pkg = match.CatalogPackage;
                    var installedVersion = pkg.InstalledVersion;

                    if (installedVersion != null)
                    {
                        var packageDef = new PackageDefinition(pkg.Id)
                        {
                            Version = installedVersion.Version
                        };
                        packages.Add(packageDef);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to process installed package at index {Index}", i);
                }

            logger.LogInformation("Retrieved {Count} installed packages", packages.Count);
            return packages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get installed packages");
            return packages;
        }
    }

    public async Task<int> ExecutePassthroughAsync(string[] args)
    {
        logger.LogDebug("Delegating passthrough command to WinGetPassthroughClient: {Args}", string.Join(" ", args));
        return await passthroughClient.ExecuteAsync(args);
    }


    private string GetComErrorMessage(int hresult)
    {
        return hresult switch
        {
            unchecked((int)0x80040154) =>
                "Class not registered (REGDB_E_CLASSNOTREG) - WinGet COM API may not be available",
            unchecked((int)0x80070002) => "File not found - Windows Package Manager may not be installed",
            unchecked((int)0x80004005) => "Unspecified error - COM server may not be running",
            unchecked((int)0x80080005) => "Server execution failed - WinGet COM server failed to start",
            _ => $"Unknown COM error: 0x{hresult:X8}"
        };
    }

    private string? GetPackageId(string[] args)
    {
        // --id フラグが指定されている場合
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--id" || args[i] == "-i")
                return args[i + 1];

        // winget.exe形式の引数解析: 最初の引数（install/uninstall）の後がパッケージID
        if (args.Length >= 2 &&
            (args[0] == "install" || args[0] == "uninstall" || args[0] == "upgrade"))
            // 2番目の引数がオプション（--で始まる）でない場合、それがパッケージID
            if (!args[1].StartsWith("--") && !args[1].StartsWith("-"))
                return args[1];

        logger.LogWarning("Package ID not specified in args: {Args}", string.Join(" ", args));
        return null;
    }


    private async Task<int> FallbackToWingetExe(string[] args, string packageId, string operation)
    {
        logger.LogInformation("Falling back to winget.exe for {Operation}: {PackageId}", operation, packageId);
        return await passthroughClient.ExecuteAsync(args);
    }
}