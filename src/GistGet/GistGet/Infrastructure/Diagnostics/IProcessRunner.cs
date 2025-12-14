using System.Diagnostics;

namespace GistGet.Infrastructure.Diagnostics;

public interface IProcessRunner
{
    Task<int> RunAsync(ProcessStartInfo startInfo);
}
