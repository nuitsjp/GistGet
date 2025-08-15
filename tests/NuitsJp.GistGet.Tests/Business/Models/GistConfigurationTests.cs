using System.Text.Json;
using NuitsJp.GistGet.Models;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Business.Models;

public class GistConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var gistId = "d239aabb67e60650fbcb2b20a8342be1";
        var fileName = "packages.yaml";

        // Act
        var config = new GistConfiguration(gistId, fileName);

        // Assert
        config.GistId.ShouldBe(gistId);
        config.FileName.ShouldBe(fileName);
        config.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        config.LastAccessedAt.ShouldBe(config.CreatedAt);
    }

    [Fact]
    public void GistId_WhenNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GistConfiguration(null!, "packages.yaml"));
    }

    [Fact]
    public void GistId_WhenEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GistConfiguration("", "packages.yaml"));
    }

    [Fact]
    public void FileName_WhenNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GistConfiguration("abc123", null!));
    }

    [Fact]
    public void FileName_WhenEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new GistConfiguration("abc123", ""));
    }

    [Fact]
    public void UpdateLastAccessed_ShouldUpdateTimestamp()
    {
        // Arrange
        var config = new GistConfiguration("abc123", "packages.yaml");
        var originalTime = config.LastAccessedAt;

        // Act
        Thread.Sleep(10); // 時刻差を作るため
        config.UpdateLastAccessed();

        // Assert
        config.LastAccessedAt.ShouldBeGreaterThan(originalTime);
    }

    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        // Arrange
        var config = new GistConfiguration("abc123", "packages.yaml");

        // Act
        var json = config.ToJson();

        // Assert
        json.ShouldNotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<GistConfiguration>(json);
        deserialized!.GistId.ShouldBe(config.GistId);
        deserialized.FileName.ShouldBe(config.FileName);
        deserialized.CreatedAt.ShouldBeInRange(config.CreatedAt.AddMilliseconds(-1),
            config.CreatedAt.AddMilliseconds(1));
        deserialized.LastAccessedAt.ShouldBeInRange(config.LastAccessedAt.AddMilliseconds(-1),
            config.LastAccessedAt.AddMilliseconds(1));
    }

    [Fact]
    public void FromJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var originalConfig = new GistConfiguration("abc123", "packages.yaml");
        var json = originalConfig.ToJson();

        // Act
        var deserializedConfig = GistConfiguration.FromJson(json);

        // Assert
        deserializedConfig.GistId.ShouldBe(originalConfig.GistId);
        deserializedConfig.FileName.ShouldBe(originalConfig.FileName);
        deserializedConfig.CreatedAt.ShouldBeInRange(originalConfig.CreatedAt.AddMilliseconds(-1),
            originalConfig.CreatedAt.AddMilliseconds(1));
        deserializedConfig.LastAccessedAt.ShouldBeInRange(originalConfig.LastAccessedAt.AddMilliseconds(-1),
            originalConfig.LastAccessedAt.AddMilliseconds(1));
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var config = new GistConfiguration("abc123", "packages.yaml");

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidGistId_ShouldThrowArgumentException(string? invalidGistId)
    {
        // Arrange
        var config = new GistConfiguration("abc123", "packages.yaml");
        // リフレクションでプライベートフィールドを変更
        var gistIdProperty = typeof(GistConfiguration).GetProperty("GistId");
        gistIdProperty?.SetValue(config, invalidGistId);

        // Act & Assert
        Should.Throw<ArgumentException>(() => config.Validate());
    }
}