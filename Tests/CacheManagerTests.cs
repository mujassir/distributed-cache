using Microsoft.Extensions.Logging;
using Moq;
using SelfHostedCacheService.Services;
using Xunit;

namespace SelfHostedCacheService.Tests;

public class CacheManagerTests
{
    private readonly CacheManager _cacheManager;
    private readonly Mock<ILogger<CacheManager>> _mockLogger;

    public CacheManagerTests()
    {
        _mockLogger = new Mock<ILogger<CacheManager>>();
        _cacheManager = new CacheManager(_mockLogger.Object);
    }

    [Fact]
    public void Set_ValidKeyValue_ReturnsTrue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        var result = _cacheManager.Set(key, value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Set_EmptyKey_ReturnsFalse()
    {
        // Arrange
        var key = "";
        var value = "test-value";

        // Act
        var result = _cacheManager.Set(key, value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Get_ExistingKey_ReturnsItem()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        _cacheManager.Set(key, value);

        // Act
        var result = _cacheManager.Get(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Get_NonExistingKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = _cacheManager.Get(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Get_ExpiredKey_ReturnsNull()
    {
        // Arrange
        var key = "expired-key";
        var value = "test-value";
        var ttl = 1; // 1 second
        _cacheManager.Set(key, value, ttl);

        // Act
        Thread.Sleep(1100); // Wait for expiration
        var result = _cacheManager.Get(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        _cacheManager.Set(key, value);

        // Act
        var result = _cacheManager.Remove(key);

        // Assert
        Assert.True(result);
        Assert.Null(_cacheManager.Get(key));
    }

    [Fact]
    public void Remove_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = _cacheManager.Remove(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Exists_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        _cacheManager.Set(key, value);

        // Act
        var result = _cacheManager.Exists(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Exists_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = _cacheManager.Exists(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Flush_ClearsAllItems()
    {
        // Arrange
        _cacheManager.Set("key1", "value1");
        _cacheManager.Set("key2", "value2");
        _cacheManager.Set("key3", "value3");

        // Act
        _cacheManager.Flush();

        // Assert
        Assert.Null(_cacheManager.Get("key1"));
        Assert.Null(_cacheManager.Get("key2"));
        Assert.Null(_cacheManager.Get("key3"));
        Assert.Equal(0, _cacheManager.GetStats().TotalKeys);
    }

    [Fact]
    public void GetAll_ReturnsAllNonExpiredItems()
    {
        // Arrange
        _cacheManager.Set("key1", "value1");
        _cacheManager.Set("key2", "value2");
        _cacheManager.Set("key3", "value3", 1); // Will expire

        Thread.Sleep(1100); // Wait for key3 to expire

        // Act
        var result = _cacheManager.GetAll();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("key1"));
        Assert.True(result.ContainsKey("key2"));
        Assert.False(result.ContainsKey("key3"));
    }

    [Fact]
    public void Search_ValidPattern_ReturnsMatchingItems()
    {
        // Arrange
        _cacheManager.Set("user:1", "John");
        _cacheManager.Set("user:2", "Jane");
        _cacheManager.Set("product:1", "Laptop");
        _cacheManager.Set("product:2", "Mouse");

        // Act
        var result = _cacheManager.Search("user:.*");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("user:1"));
        Assert.True(result.ContainsKey("user:2"));
        Assert.False(result.ContainsKey("product:1"));
        Assert.False(result.ContainsKey("product:2"));
    }

    [Fact]
    public void Search_InvalidPattern_ReturnsEmptyDictionary()
    {
        // Arrange
        _cacheManager.Set("key1", "value1");

        // Act
        var result = _cacheManager.Search("[invalid");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CleanupExpiredItems_RemovesExpiredItems()
    {
        // Arrange
        _cacheManager.Set("key1", "value1"); // No TTL
        _cacheManager.Set("key2", "value2", 1); // 1 second TTL
        _cacheManager.Set("key3", "value3", 1); // 1 second TTL

        Thread.Sleep(1100); // Wait for expiration

        // Act
        var removedCount = _cacheManager.CleanupExpiredItems();

        // Assert
        Assert.Equal(2, removedCount);
        Assert.NotNull(_cacheManager.Get("key1"));
        Assert.Null(_cacheManager.Get("key2"));
        Assert.Null(_cacheManager.Get("key3"));
    }

    [Fact]
    public void GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        _cacheManager.Set("key1", "value1");
        _cacheManager.Set("key2", "value2");
        _cacheManager.Get("key1"); // Hit
        _cacheManager.Get("key3"); // Miss

        // Act
        var stats = _cacheManager.GetStats();

        // Assert
        Assert.Equal(2, stats.TotalKeys);
        Assert.Equal(1, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(0.5, stats.HitRate);
        Assert.True(stats.UptimeSeconds >= 0);
        Assert.True(stats.MemoryUsageBytes > 0);
    }

    [Fact]
    public void Set_WithTTL_ItemExpiresCorrectly()
    {
        // Arrange
        var key = "ttl-key";
        var value = "ttl-value";
        var ttl = 2; // 2 seconds

        // Act
        _cacheManager.Set(key, value, ttl);
        var itemBeforeExpiry = _cacheManager.Get(key);
        
        Thread.Sleep(2100); // Wait for expiration
        var itemAfterExpiry = _cacheManager.Get(key);

        // Assert
        Assert.NotNull(itemBeforeExpiry);
        Assert.Null(itemAfterExpiry);
    }

    [Fact]
    public void ConcurrentOperations_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyCount = 100;

        // Act - Concurrent sets
        for (int i = 0; i < keyCount; i++)
        {
            var key = $"key-{i}";
            var value = $"value-{i}";
            tasks.Add(Task.Run(() => _cacheManager.Set(key, value)));
        }

        Task.WaitAll(tasks.ToArray());
        tasks.Clear();

        // Act - Concurrent gets
        var results = new bool[keyCount];
        for (int i = 0; i < keyCount; i++)
        {
            var index = i;
            var key = $"key-{i}";
            tasks.Add(Task.Run(() => results[index] = _cacheManager.Get(key) != null));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(keyCount, _cacheManager.GetStats().TotalKeys);
        Assert.All(results, result => Assert.True(result));
    }
}
