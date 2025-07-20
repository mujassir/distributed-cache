using System.Text.Json.Serialization;

namespace SelfHostedCacheService.Models;

/// <summary>
/// Generic response model for cache operations
/// </summary>
public class CacheResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static CacheResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new CacheResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static CacheResponse<T> ErrorResponse(string message)
    {
        return new CacheResponse<T>
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Response model for cache item retrieval
/// </summary>
public class CacheItemResponse
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("ttl")]
    public int? RemainingTtlSeconds { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
