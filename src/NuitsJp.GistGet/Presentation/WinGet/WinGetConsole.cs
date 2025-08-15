using NuitsJp.GistGet.Presentation.Console;

namespace NuitsJp.GistGet.Presentation.WinGet;

/// <summary>
/// WinGetコマンド用のコンソール実装
/// </summary>
public class WinGetConsole : ConsoleBase, IWinGetConsole
{
    public void NotifyInstallStarting(string packageId)
    {
        System.Console.WriteLine($"パッケージをインストールしています: {packageId}");
    }

    public void NotifyUninstallStarting(string packageId)
    {
        System.Console.WriteLine($"パッケージをアンインストールしています: {packageId}");
    }

    public void NotifyUpgradeStarting(string packageId)
    {
        System.Console.WriteLine($"パッケージをアップグレードしています: {packageId}");
    }

    public void NotifyOperationSuccess(string operation, string packageId)
    {
        System.Console.WriteLine($"✓ {operation}が完了しました: {packageId}");
    }

    public void NotifyGistUpdateStarting()
    {
        System.Console.WriteLine("Gistパッケージリストを更新しています...");
    }

    public void NotifyGistUpdateSuccess()
    {
        System.Console.WriteLine("✓ Gistパッケージリストを更新しました");
    }

    public bool ConfirmRebootWithPackageList(List<string> packagesRequiringReboot)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("以下のパッケージは再起動が必要です:");
        foreach (var package in packagesRequiringReboot)
        {
            System.Console.WriteLine($"  - {package}");
        }
        System.Console.WriteLine();
        System.Console.Write("今すぐ再起動しますか? (y/N): ");

        var response = System.Console.ReadLine();
        return response?.ToLowerInvariant() is "y" or "yes";
    }

    public void NotifyRebootExecuting()
    {
        System.Console.WriteLine("システムを再起動しています...");
    }

    protected override void WriteErrorLine(string message)
    {
        System.Console.Error.WriteLine(message);
    }

    protected override void WriteWarningLine(string message)
    {
        System.Console.WriteLine(message);
    }

    protected override void WriteInfoLine(string message)
    {
        System.Console.WriteLine(message);
    }
}