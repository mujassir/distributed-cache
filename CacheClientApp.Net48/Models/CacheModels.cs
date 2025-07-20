using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CacheClientApp.Net48.Models
{
    /// <summary>
    /// Generic response model for cache operations
    /// </summary>
    public class CacheResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Response model for cache item retrieval
    /// </summary>
    public class CacheItemResponse
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("ttl")]
        public int? RemainingTtlSeconds { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request model for adding cache items
    /// </summary>
    public class AddCacheRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("ttl")]
        public int? TtlSeconds { get; set; }
    }

    /// <summary>
    /// Cache statistics model
    /// </summary>
    public class CacheStats
    {
        [JsonProperty("total_keys")]
        public int TotalKeys { get; set; }

        [JsonProperty("hits")]
        public long Hits { get; set; }

        [JsonProperty("misses")]
        public long Misses { get; set; }

        [JsonProperty("hit_rate")]
        public double HitRate { get; set; }

        [JsonProperty("uptime_seconds")]
        public long UptimeSeconds { get; set; }

        [JsonProperty("memory_usage_bytes")]
        public long MemoryUsageBytes { get; set; }

        [JsonProperty("expired_keys_cleaned")]
        public long ExpiredKeysCleanedUp { get; set; }

        [JsonProperty("last_cleanup")]
        public DateTime? LastCleanup { get; set; }

        [JsonProperty("server_start_time")]
        public DateTime ServerStartTime { get; set; }
    }

    /// <summary>
    /// Named Pipe request model
    /// </summary>
    public class NamedPipeRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("ttl")]
        public int? TtlSeconds { get; set; }

        [JsonProperty("pattern")]
        public string Pattern { get; set; }
    }

    /// <summary>
    /// Health check response
    /// </summary>
    public class HealthResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }
    }
}
