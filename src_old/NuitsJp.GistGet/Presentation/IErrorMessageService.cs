using System.Runtime.InteropServices;

namespace NuitsJp.GistGet.Presentation;

/// <summary>
/// エラーメッセージ表示サービスインターフェース
/// </summary>
public interface IErrorMessageService
{
    void HandleComException(COMException comEx);
    void HandleNetworkException(HttpRequestException httpEx);
    void HandlePackageNotFoundException(InvalidOperationException invEx);
    void HandleUnexpectedException(Exception ex);
}