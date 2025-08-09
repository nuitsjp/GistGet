namespace NuitsJp.GistGet.Commands;

public class PinCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Pin command executed");
        await Task.Delay(100);
        return 0;
    }
}

