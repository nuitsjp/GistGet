using Windows.Devices.Usb;

namespace GistGet.Command;

public abstract class CommandBase(IUserInterface userInterface) : ICommand
{
    public abstract string Name { get; }
    protected abstract string Help { get; }
    public Task RunAsync(string[] args)
    {
        if (args.Contains("-?")
            || args.Contains("-h")
            || args.Contains("--help"))
        {
            userInterface.WriteLine(Help);
        }

        return RunInnerAsync(args);
    }

    protected abstract Task RunInnerAsync(string[] args);
}