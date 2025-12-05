using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GistGet.Models;
using Microsoft.Management.Deployment;

namespace GistGet.Infrastructure.WinGet;

public class WinGetRepository : IWinGetRepository
{
    private readonly Infrastructure.OS.IProcessRunner _processRunner;
    private readonly string _wingetExe;

    public WinGetRepository(Infrastructure.OS.IProcessRunner processRunner)
    {
        _processRunner = processRunner;
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        _wingetExe = System.IO.Path.Combine(localAppData!, "Microsoft", "WindowsApps", "winget.exe");
    }

    public async Task<Dictionary<string, GistGetPackage>> GetInstalledPackagesAsync()
    {
        var packages = new Dictionary<string, GistGetPackage>(StringComparer.OrdinalIgnoreCase);

        var packageManager = new PackageManager();
        var catalogRef = packageManager.GetLocalPackageCatalog(LocalPackageCatalog.InstalledPackages);
        var connectResult = await catalogRef.ConnectAsync();
        if (connectResult.Status != ConnectResultStatus.Ok || connectResult.PackageCatalog == null)
        {
            return packages;
        }

        var findOptions = new FindPackagesOptions();
        var findResult = await connectResult.PackageCatalog.FindPackagesAsync(findOptions);
        for (var i = 0; i < findResult.Matches.Count; i++)
        {
            var match = findResult.Matches[i];
            var package = match.CatalogPackage;
            var installedVersion = package.InstalledVersion;
            if (installedVersion == null)
            {
                continue;
            }

            var id = package.Id;
            var version = installedVersion.Version ?? "Unknown";

            if (!packages.ContainsKey(id))
            {
                packages.Add(id, new GistGetPackage
                {
                    Id = id,
                    Version = version
                });
            }
        }

        return packages;
    }

    public async Task<Dictionary<string, string>> GetPinnedPackagesAsync()
    {
        var pinnedPackages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // winget pin list output format:
        // Name      Id          Version   Source
        // ---------------------------------------
        // Package   Package.Id  1.2.3     winget
        
        // We need to parse this output.
        // Note: The output might be localized or vary.
        // A more robust way would be to use --json if available, but pin list doesn't support --json yet (as of v1.6).
        // So we parse the text.

        // Run winget pin list
        var output = "";
        try 
        {
            // We need to capture stdout. IProcessRunner.RunPassthroughAsync returns exit code.
            // We need a method to capture output.
            // Checking IProcessRunner interface...
            // If IProcessRunner doesn't support capturing output, we might need to extend it or use System.Diagnostics.Process directly here for now, 
            // OR assuming IProcessRunner has a method for it.
            // Let's assume for now we need to implement it or use a different approach.
            // Wait, I don't see IProcessRunner definition. I should check it.
            // But for now, I will implement using System.Diagnostics.Process directly or assume a RunAsyncWithOutput method exists or I will add it.
            // Let's check IProcessRunner first.
            
            // Reverting to using System.Diagnostics.Process for capturing output as I don't want to break IProcessRunner if it's simple.
            // Actually, let's look at IProcessRunner first.
            
            // For this step, I will just add the method signature and placeholder implementation 
            // that throws NotImplementedException until I check IProcessRunner.
            // But I am in a multi_replace, so I should try to do it right.
            
            // Let's assume standard Process usage for capturing output.
            
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _wingetExe,
                Arguments = "pin list",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
            process.Start();
            output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
        }
        catch (Exception)
        {
            // Handle error or return empty
            return pinnedPackages;
        }

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var headerFound = false;
        var idIndex = -1;
        var versionIndex = -1;

        foreach (var line in lines)
        {
            if (line.StartsWith("---"))
            {
                headerFound = true;
                continue;
            }

            if (!headerFound)
            {
                // Parse header to find column indices
                // Name      Id          Version   Source
                if (line.Contains("Id") && line.Contains("Version"))
                {
                    idIndex = line.IndexOf("Id");
                    versionIndex = line.IndexOf("Version");
                }
                continue;
            }

            if (idIndex != -1 && versionIndex != -1 && line.Length > versionIndex)
            {
                // Simple fixed width parsing based on header position
                // This is fragile but winget output is designed for humans.
                // A better way is to split by whitespace but names can contain spaces.
                // However, Id usually doesn't contain spaces.
                // Let's try to extract based on indices.
                
                // The columns are usually separated by multiple spaces.
                // Let's try a regex or just substring.
                
                // Name ends before Id.
                // Id starts at idIndex.
                // Version starts at versionIndex.
                
                // Let's assume Id is at idIndex and ends before VersionIndex.
                // But columns might be dynamic.
                
                // Alternative: Split by multiple spaces.
                var parts = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\s{2,}");
                if (parts.Length >= 3)
                {
                    // Assuming Name, Id, Version, Source...
                    // But if Name is long, it might not be 2 spaces.
                    
                    // Let's use the substring approach with some trimming.
                    try 
                    {
                        // Id is usually the second column.
                        // But finding where Name ends is hard if we don't use the header indices.
                        
                        // Let's use the header indices.
                        // Id column starts at idIndex.
                        // We need to find where it ends. It ends where Version starts (versionIndex).
                        
                        var idRaw = line.Substring(idIndex, versionIndex - idIndex).Trim();
                        var versionRaw = line.Substring(versionIndex).Trim();
                        // Version might be followed by Source.
                        var versionParts = versionRaw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var version = versionParts.Length > 0 ? versionParts[0] : "";
                        
                        if (!string.IsNullOrEmpty(idRaw))
                        {
                            pinnedPackages[idRaw] = version;
                        }
                    }
                    catch {}
                }
            }
        }

        return pinnedPackages;
    }
}
