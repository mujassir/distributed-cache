using System.Text.Json.Serialization;

namespace SelfHostedCacheService.Models;

/// <summary>
/// Request model for Named Pipe and HTTP operations
/// </summary>
public class CacheRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("ttl")]
    public int? TtlSeconds { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }
}

/// <summary>
/// Request model for adding cache items via HTTP
/// </summary>
public class AddCacheRequest
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object Value { get; set; } = null!;

    [JsonPropertyName("ttl")]
    public int? TtlSeconds { get; set; }
}

/// <summary>
/// Search request model
/// </summary>
public class SearchRequest
{
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;
}
