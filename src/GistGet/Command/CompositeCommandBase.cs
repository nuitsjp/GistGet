namespace GistGet.Command;

public abstract class CompositeCommandBase : ICommand
{
    private readonly IUserInterface _userInterface;
    private readonly IReadOnlyDictionary<string, ICommand> _commands;

    protected CompositeCommandBase(
        IUserInterface userInterface,
        IEnumerable<ICommand> commands)
    {
        _userInterface = userInterface;
        _commands = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public abstract string Name { get; }
    protected abstract string Help { get; }

    public async Task RunAsync(string[] args)
    {
        if (args.Contains("-?")
            || args.Contains("-h")
            || args.Contains("--help"))
        {
            _userInterface.WriteLine(Help);
        }

        var commandName = args.FirstOrDefault();

        if (commandName is not null && _commands.TryGetValue(commandName, out var command))
        {
            await command.RunAsync(args.Skip(1).ToArray());
        }

        _userInterface.WriteLine(Help);
    }
}