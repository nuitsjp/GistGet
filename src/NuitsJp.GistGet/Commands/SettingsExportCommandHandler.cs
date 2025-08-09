namespace NuitsJp.GistGet.Commands;

public class SettingsExportCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Settings export command executed");
        await Task.Delay(100);
        return 0;
    }
}

