namespace GistGet;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// 認証資格情報の永続化を担当するサービスインターフェース。
/// Windows資格情報マネージャーを使用してGitHubトークンを安全に保存・取得します。
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// 保存されている資格情報の取得を試みます。
    /// </summary>
    /// <param name="credential">取得した資格情報。取得できない場合はnull。</param>
    /// <returns>資格情報が存在する場合はtrue、存在しない場合はfalse。</returns>
    bool TryGetCredential([NotNullWhen(true)] out Credential credential);

    /// <summary>
    /// 資格情報をWindows資格情報マネージャーに保存します。
    /// </summary>
    /// <param name="credential">保存する資格情報（ユーザー名とトークン）</param>
    /// <returns>保存が成功した場合はtrue。</returns>
    bool SaveCredential(Credential credential);

    /// <summary>
    /// 保存されている資格情報を削除します。
    /// ログアウト時に呼び出されます。
    /// </summary>
    /// <returns>削除が成功した場合はtrue。</returns>
    bool DeleteCredential();
}