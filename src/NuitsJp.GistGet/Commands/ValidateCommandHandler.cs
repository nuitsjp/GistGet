namespace NuitsJp.GistGet.Commands;

public class ValidateCommandHandler : BaseCommandHandler
{
    public override async Task<int> ExecuteAsync()
    {
        Console.WriteLine("Validate command executed");
        await Task.Delay(100);
        return 0;
    }
}

