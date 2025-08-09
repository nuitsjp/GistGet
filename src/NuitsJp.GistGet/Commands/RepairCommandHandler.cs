namespace NuitsJp.GistGet.Commands;

public class RepairCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Repair command executed");
        await Task.Delay(100);
        return 0;
    }
}

