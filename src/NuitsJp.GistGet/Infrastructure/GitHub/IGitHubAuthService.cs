using Octokit;

namespace NuitsJp.GistGet.Infrastructure.GitHub;

/// <summary>
/// GitHub認証サービスのインターフェース
/// </summary>

public interface IGitHubAuthService
{
    /// <summary>
    /// Device Flow認証を実行し、アクセストークンを取得・保存する
    /// </summary>
    Task<bool> AuthenticateAsync();

    /// <summary>
    /// 保存されたトークンの有効性を確認する
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// 認証済みのGitHubクライアントを取得する
    /// </summary>
    Task<GitHubClient?> GetAuthenticatedClientAsync();

    /// <summary>
    /// 認証状態を表示する
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    Task ShowAuthStatusAsync();

    /// <summary>
    /// 保存されたトークンを削除してログアウトする
    /// </summary>
    Task<bool> LogoutAsync();
}