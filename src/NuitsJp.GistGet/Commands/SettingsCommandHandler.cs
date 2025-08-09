namespace NuitsJp.GistGet.Commands;

public class SettingsCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Settings command executed");
        await Task.Delay(100);
        return 0;
    }
}

