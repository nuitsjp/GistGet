namespace NuitsJp.GistGet.Abstractions;

/// <summary>
/// エラーメッセージ表示サービスインターフェース
/// </summary>
public interface IErrorMessageService
{
    void HandleComException(System.Runtime.InteropServices.COMException comEx);
    void HandleNetworkException(HttpRequestException httpEx);
    void HandlePackageNotFoundException(InvalidOperationException invEx);
    void HandleUnexpectedException(Exception ex);
}