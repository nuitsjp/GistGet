using System.Text.Json;

namespace NuitsJp.GistGet.Models;

public class GistConfiguration
{
    public string GistId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }

    public GistConfiguration(string gistId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(gistId))
            throw new ArgumentException("Gist ID cannot be null or empty", nameof(gistId));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        GistId = gistId;
        FileName = fileName;
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = CreatedAt;
    }

    // JSON デシリアライゼーション用のパラメータなしコンストラクタ
    public GistConfiguration()
    {
    }

    public void UpdateLastAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public static GistConfiguration FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        try
        {
            var config = JsonSerializer.Deserialize<GistConfiguration>(json);
            if (config == null)
                throw new InvalidOperationException("Deserialized configuration is null");

            config.Validate();
            return config;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(json), ex);
        }
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(GistId))
            throw new ArgumentException("Gist ID cannot be null or empty");

        if (string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException("File name cannot be null or empty");
    }
}