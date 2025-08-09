namespace NuitsJp.GistGet.Commands;

public class Dscv3CommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Dscv3 command executed");
        await Task.Delay(100);
        return 0;
    }
}

