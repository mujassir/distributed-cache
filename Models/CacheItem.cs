namespace SelfHostedCacheService.Models;

/// <summary>
/// Represents a cache entry with TTL support
/// </summary>
public class CacheItem
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? TtlSeconds { get; set; }

    public CacheItem()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public CacheItem(string key, object value, int? ttlSeconds = null)
    {
        Key = key;
        Value = value;
        CreatedAt = DateTime.UtcNow;
        TtlSeconds = ttlSeconds;
        
        if (ttlSeconds.HasValue && ttlSeconds.Value > 0)
        {
            ExpiresAt = CreatedAt.AddSeconds(ttlSeconds.Value);
        }
    }

    /// <summary>
    /// Checks if the cache item has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>
    /// Gets the remaining TTL in seconds
    /// </summary>
    public int? RemainingTtlSeconds
    {
        get
        {
            if (!ExpiresAt.HasValue) return null;
            var remaining = (int)(ExpiresAt.Value - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, remaining);
        }
    }
}
