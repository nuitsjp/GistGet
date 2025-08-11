using System.Text.RegularExpressions;
using NuitsJp.GistGet.Models;

namespace NuitsJp.GistGet.Services;

public class GistInputService
{
    private static readonly Regex GistIdPattern = new(@"^[a-fA-F0-9]{32}$", RegexOptions.Compiled);
    private static readonly Regex GistUrlPattern = new(@"(?:https://gist\.github\.com/[^/]+/)?([a-fA-F0-9]{32})", RegexOptions.Compiled);
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private static readonly string[] ReservedFileNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

    public void ValidateGistId(string gistId)
    {
        if (string.IsNullOrWhiteSpace(gistId))
        {
            throw new ArgumentException("Gist ID cannot be null or empty", nameof(gistId));
        }

        if (!GistIdPattern.IsMatch(gistId))
        {
            throw new ArgumentException("Gist ID must be a 32-character hexadecimal string", nameof(gistId));
        }
    }

    public void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
        }

        // 不正な文字をチェック
        if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
        {
            throw new ArgumentException($"File name contains invalid characters. Invalid characters: {string.Join(", ", InvalidFileNameChars)}", nameof(fileName));
        }

        // 予約済みファイル名をチェック (Windows)
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        if (ReservedFileNames.Contains(fileNameWithoutExtension))
        {
            throw new ArgumentException($"File name '{fileName}' is a reserved name and cannot be used", nameof(fileName));
        }

        // ピリオドで終わるファイル名は無効
        if (fileName.EndsWith('.'))
        {
            throw new ArgumentException("File name cannot end with a period", nameof(fileName));
        }
    }

    public string ExtractGistIdFromUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var match = GistUrlPattern.Match(input);
        if (!match.Success)
        {
            throw new ArgumentException("Invalid Gist ID or URL format", nameof(input));
        }

        return match.Groups[1].Value;
    }

    public GistConfiguration CreateConfiguration(string gistId, string fileName)
    {
        ValidateGistId(gistId);
        ValidateFileName(fileName);

        return new GistConfiguration(gistId, fileName);
    }

    public string GetDefaultFileName()
    {
        return "packages.yaml";
    }

    public string GetGistCreationInstructions()
    {
        return """
        GitHub Gist を作成する手順:
        
        1. https://gist.github.com にアクセス
        2. 「Create a new gist」をクリック
        3. ファイル名に「packages.yaml」を入力
        4. 内容は空のままでも構いません（後で自動更新されます）
        5. 「Create public gist」または「Create secret gist」をクリック
        6. 作成後のURLからGist IDを取得してください
           例: https://gist.github.com/username/abc123... の「abc123...」部分
        
        注意: プライベートGistを作成する場合、GitHubの認証トークンに適切な権限が必要です
        """;
    }

    public string FormatGistUrl(string gistId)
    {
        ValidateGistId(gistId);
        return $"https://gist.github.com/{gistId}";
    }
}