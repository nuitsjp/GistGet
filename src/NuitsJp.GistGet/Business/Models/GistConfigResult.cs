namespace NuitsJp.GistGet.Business.Models;

public class GistConfigResult
{
    public bool IsSuccess { get; init; }
    public string? GistId { get; init; }
    public string? FileName { get; init; }
    public string? ErrorMessage { get; init; }

    public static GistConfigResult Success(string gistId, string fileName)
    {
        return new GistConfigResult
        {
            IsSuccess = true,
            GistId = gistId,
            FileName = fileName
        };
    }

    public static GistConfigResult Failure(string errorMessage)
    {
        return new GistConfigResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}