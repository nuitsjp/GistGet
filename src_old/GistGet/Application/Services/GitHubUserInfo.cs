using System;

namespace GistGet.Application.Services
{
    public class GitHubUserInfo
    {
        public string Login { get; set; } = "";
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
