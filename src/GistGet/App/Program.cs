
using GistGet;
using GistGet.Diagnostics;

var router =
    new CommandRouter(
        new WinGetPassthroughRunner());
await router.RunAsync(args);