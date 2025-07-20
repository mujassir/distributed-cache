using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SelfHostedCacheService.Models;

namespace SelfHostedCacheService.Services;

/// <summary>
/// High-performance in-memory cache manager with TTL support
/// </summary>
public class CacheManager
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache;
    private readonly ILogger<CacheManager> _logger;
    private readonly DateTime _serverStartTime;
    
    // Performance counters
    private long _hits = 0;
    private long _misses = 0;
    private long _expiredKeysCleanedUp = 0;
    private DateTime? _lastCleanup;

    public CacheManager(ILogger<CacheManager> logger)
    {
        _cache = new ConcurrentDictionary<string, CacheItem>();
        _logger = logger;
        _serverStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds or updates a cache item
    /// </summary>
    public bool Set(string key, object value, int? ttlSeconds = null)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            var cacheItem = new CacheItem(key, value, ttlSeconds);
            _cache.AddOrUpdate(key, cacheItem, (k, v) => cacheItem);
            
            _logger.LogDebug("Cache SET: {Key} with TTL: {TTL}s", key, ttlSeconds);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache item for key: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Gets a cache item by key
    /// </summary>
    public CacheItem? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Interlocked.Increment(ref _misses);
            return null;
        }

        if (_cache.TryGetValue(key, out var item))
        {
            if (item.IsExpired)
            {
                // Remove expired item immediately
                _cache.TryRemove(key, out _);
                Interlocked.Increment(ref _misses);
                _logger.LogDebug("Cache MISS (expired): {Key}", key);
                return null;
            }

            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Cache HIT: {Key}", key);
            return item;
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache MISS: {Key}", key);
        return null;
    }

    /// <summary>
    /// Removes a cache item by key
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        var removed = _cache.TryRemove(key, out _);
        if (removed)
        {
            _logger.LogDebug("Cache REMOVE: {Key}", key);
        }
        return removed;
    }

    /// <summary>
    /// Checks if a key exists and is not expired
    /// </summary>
    public bool Exists(string key)
    {
        return Get(key) != null;
    }

    /// <summary>
    /// Clears all cache items
    /// </summary>
    public void Flush()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Cache FLUSH: Cleared {Count} items", count);
    }

    /// <summary>
    /// Gets all cache items (non-expired)
    /// </summary>
    public Dictionary<string, CacheItem> GetAll()
    {
        var result = new Dictionary<string, CacheItem>();
        var expiredKeys = new List<string>();

        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired)
            {
                expiredKeys.Add(kvp.Key);
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        // Clean up expired items found during enumeration
        foreach (var expiredKey in expiredKeys)
        {
            _cache.TryRemove(expiredKey, out _);
        }

        return result;
    }

    /// <summary>
    /// Searches cache items by regex pattern
    /// </summary>
    public Dictionary<string, CacheItem> Search(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return new Dictionary<string, CacheItem>();

        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var result = new Dictionary<string, CacheItem>();
            var expiredKeys = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                    continue;
                }

                if (regex.IsMatch(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            // Clean up expired items
            foreach (var expiredKey in expiredKeys)
            {
                _cache.TryRemove(expiredKey, out _);
            }

            _logger.LogDebug("Cache SEARCH: Pattern '{Pattern}' found {Count} matches", pattern, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cache with pattern: {Pattern}", pattern);
            return new Dictionary<string, CacheItem>();
        }
    }

    /// <summary>
    /// Removes expired cache items
    /// </summary>
    public int CleanupExpiredItems()
    {
        var expiredKeys = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _cache)
        {
            if (kvp.Value.ExpiresAt.HasValue && now > kvp.Value.ExpiresAt.Value)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        var removedCount = 0;
        foreach (var key in expiredKeys)
        {
            if (_cache.TryRemove(key, out _))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            Interlocked.Add(ref _expiredKeysCleanedUp, removedCount);
            _lastCleanup = DateTime.UtcNow;
            _logger.LogDebug("Cache CLEANUP: Removed {Count} expired items", removedCount);
        }

        return removedCount;
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public CacheStats GetStats()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        var uptime = (long)(DateTime.UtcNow - _serverStartTime).TotalSeconds;

        return new CacheStats
        {
            TotalKeys = _cache.Count,
            Hits = _hits,
            Misses = _misses,
            UptimeSeconds = uptime,
            MemoryUsageBytes = memoryUsage,
            ExpiredKeysCleanedUp = _expiredKeysCleanedUp,
            LastCleanup = _lastCleanup,
            ServerStartTime = _serverStartTime
        };
    }
}
