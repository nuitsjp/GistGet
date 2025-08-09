namespace NuitsJp.GistGet.Commands;

public class UninstallCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Uninstall command executed");
        await Task.Delay(100);
        return 0;
    }
}

