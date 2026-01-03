// GistGet launcher - launches NuitsJp.GistGet.exe as a separate process
using System.Diagnostics;

// Get the directory where GistGet.exe actually resides (following symlinks)
// Use Environment.ProcessPath for single-file app compatibility
var processPath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName
    ?? throw new InvalidOperationException("Could not determine process path");

// Resolve symlink to get the actual file location
var fileInfo = new FileInfo(processPath);
var actualPath = fileInfo.LinkTarget != null
    ? Path.GetFullPath(fileInfo.LinkTarget, Path.GetDirectoryName(processPath)!)
    : processPath;

var assemblyDirectory = Path.GetDirectoryName(actualPath) ?? AppContext.BaseDirectory;
var exePath = Path.Combine(assemblyDirectory, "NuitsJp.GistGet.exe");

var startInfo = new ProcessStartInfo
{
    FileName = exePath,
    UseShellExecute = false,
    WorkingDirectory = assemblyDirectory,
};

foreach (var arg in args)
{
    startInfo.ArgumentList.Add(arg);
}

using var process = Process.Start(startInfo);
if (process == null)
{
    await Console.Error.WriteLineAsync("Failed to start NuitsJp.GistGet.exe");
    return 1;
}

await process.WaitForExitAsync();
return process.ExitCode;
