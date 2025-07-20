using System.Text.Json.Serialization;

namespace SelfHostedCacheService.Models;

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStats
{
    [JsonPropertyName("total_keys")]
    public int TotalKeys { get; set; }

    [JsonPropertyName("hits")]
    public long Hits { get; set; }

    [JsonPropertyName("misses")]
    public long Misses { get; set; }

    [JsonPropertyName("hit_rate")]
    public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0.0;

    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; set; }

    [JsonPropertyName("memory_usage_bytes")]
    public long MemoryUsageBytes { get; set; }

    [JsonPropertyName("expired_keys_cleaned")]
    public long ExpiredKeysCleanedUp { get; set; }

    [JsonPropertyName("last_cleanup")]
    public DateTime? LastCleanup { get; set; }

    [JsonPropertyName("server_start_time")]
    public DateTime ServerStartTime { get; set; }
}
