using NuitsJp.GistGet;

var host = AppHost.CreateHost();

try
{
    var app = new RunnerApplication();
    return await app.RunAsync(host, args);
}
finally
{
    host.Dispose();
}