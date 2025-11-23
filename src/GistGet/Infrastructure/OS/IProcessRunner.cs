using System.Threading.Tasks;

namespace GistGet.Infrastructure.OS;

public interface IProcessRunner
{
    Task<(int ExitCode, string Output, string Error)> RunAsync(string fileName, string arguments, bool redirectOutput = true);
    Task RunPassthroughAsync(string fileName, string arguments);
}
