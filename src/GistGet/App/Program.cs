// See https://aka.ms/new-console-template for more information
using GistGet;
using GistGet.Com;

Console.WriteLine("WinGet Service Test");

var service = new WinGetService();

// Test 1: jqlang.jq (更新対象あり)
Console.WriteLine("\n--- Test 1: jqlang.jq ---");
var jq = await service.FindByIdAsync(new PackageId("jqlang.jq"));
if (jq != null)
{
    Console.WriteLine($"Name: {jq.Name}");
    Console.WriteLine($"Id: {jq.Id}");
    Console.WriteLine($"Version: {jq.Version}");
    Console.WriteLine($"UsableVersion: {jq.UsableVersion?.ToString() ?? "(null)"}");
}
else
{
    Console.WriteLine("Not found");
}

// Test 2: Microsoft.VisualStudioCode (更新なし)
Console.WriteLine("\n--- Test 2: Microsoft.VisualStudioCode ---");
var vscode = await service.FindByIdAsync(new PackageId("Microsoft.VisualStudioCode"));
if (vscode != null)
{
    Console.WriteLine($"Name: {vscode.Name}");
    Console.WriteLine($"Id: {vscode.Id}");
    Console.WriteLine($"Version: {vscode.Version}");
    Console.WriteLine($"UsableVersion: {vscode.UsableVersion?.ToString() ?? "(null)"}");
}
else
{
    Console.WriteLine("Not found");
}

// Test 3: 存在しないパッケージ
Console.WriteLine("\n--- Test 3: NonExisting.Package ---");
var nonExisting = await service.FindByIdAsync(new PackageId("NonExisting.Package.Id.12345"));
Console.WriteLine(nonExisting == null ? "Correctly returned null" : "Unexpectedly found package");
