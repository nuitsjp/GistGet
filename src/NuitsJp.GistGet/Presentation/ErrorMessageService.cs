using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Presentation;

/// <summary>
/// エラーメッセージ表示サービス（統一化された処理）
/// </summary>
public class ErrorMessageService(ILogger<ErrorMessageService> logger) : IErrorMessageService
{

    public void HandleComException(COMException comEx)
    {
        logger.LogError(comEx, "COM API error occurred");

        var userMessage = comEx.HResult switch
        {
            -2147024891 => "管理者権限が必要です。PowerShellを管理者として実行してください。",
            -2147023728 => "WinGet COM APIが利用できません。Windows Package Managerが正しくインストールされていることを確認してください。",
            _ => $"システムエラーが発生しました。詳細: {comEx.Message}"
        };

        logger.LogError("エラー: {UserMessage}", userMessage);
    }

    public void HandleNetworkException(HttpRequestException httpEx)
    {
        logger.LogError(httpEx, "Network error occurred");
        logger.LogError("エラー: インターネット接続を確認してください。プロキシ設定が必要な場合があります。");
    }

    public void HandlePackageNotFoundException(InvalidOperationException invEx)
    {
        logger.LogError(invEx, "Package not found error");
        var packageName = ExtractPackageNameFromMessage(invEx.Message);
        if (!string.IsNullOrEmpty(packageName))
            logger.LogError("エラー: パッケージ '{PackageName}' が見つかりません。正しいパッケージIDを確認してください。", packageName);
        else
            logger.LogError("エラー: 指定されたパッケージが見つかりません。正しいパッケージIDを確認してください。");
    }

    public void HandleUnexpectedException(Exception ex)
    {
        logger.LogError(ex, "Unexpected error occurred");
        logger.LogError("予期しないエラーが発生しました: {ErrorMessage}", ex.Message);
    }

    private static string? ExtractPackageNameFromMessage(string message)
    {
        // "Package 'PackageName' not found" の形式から PackageName を抽出
        var match = Regex.Match(message, "Package '([^']+)' not found");
        return match.Success ? match.Groups[1].Value : null;
    }
}