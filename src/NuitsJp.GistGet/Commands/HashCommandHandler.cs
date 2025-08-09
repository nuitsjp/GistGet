namespace NuitsJp.GistGet.Commands;

public class HashCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Hash command executed");
        await Task.Delay(100);
        return 0;
    }
}

