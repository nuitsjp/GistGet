using Moq;
using Shouldly;

namespace GistGet;

public class GistServiceTests
{
    private readonly GistService _target;

    public GistServiceTests()
    {
        _target = new GistService();
    }
}
