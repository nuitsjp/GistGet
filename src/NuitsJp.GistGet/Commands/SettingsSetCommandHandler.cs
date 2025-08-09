namespace NuitsJp.GistGet.Commands;

public class SettingsSetCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Settings set command executed");
        await Task.Delay(100);
        return 0;
    }
}

