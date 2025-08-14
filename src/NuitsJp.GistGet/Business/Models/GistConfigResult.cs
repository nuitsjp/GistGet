namespace NuitsJp.GistGet.Business.Models
{
    public class GistConfigResult
    {
        public bool IsSuccess { get; set; }
        public string? GistId { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }

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
}