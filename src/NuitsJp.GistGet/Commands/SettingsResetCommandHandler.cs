namespace NuitsJp.GistGet.Commands;

public class SettingsResetCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Settings reset command executed");
        await Task.Delay(100);
        return 0;
    }
}

