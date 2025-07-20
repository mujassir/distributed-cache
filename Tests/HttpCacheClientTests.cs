using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SelfHostedCacheService.Clients;
using SelfHostedCacheService.Services;
using Xunit;

namespace SelfHostedCacheService.Tests;

public class HttpCacheClientTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;
    private readonly HttpCacheClient _cacheClient;

    public HttpCacheClientTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override configuration for testing
                services.Configure<Microsoft.Extensions.Configuration.IConfiguration>(config =>
                {
                    config["CacheSettings:EnableHttpApi"] = "true";
                    config["CacheSettings:EnableNamedPipes"] = "false";
                });
            });
        });

        _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", "test-key-67890");
        
        _cacheClient = new HttpCacheClient(_httpClient, "http://localhost");
    }

    [Fact]
    public async Task SetAndGet_ValidKeyValue_Success()
    {
        // Arrange
        var key = "test-key-http";
        var value = "test-value-http";

        // Act
        var setResult = await _cacheClient.SetAsync(key, value);
        var getResult = await _cacheClient.GetAsync(key);

        // Assert
        Assert.True(setResult);
        Assert.NotNull(getResult);
        Assert.Equal(key, getResult.Key);
        Assert.Equal(value, getResult.Value.ToString());
    }

    [Fact]
    public async Task Get_NonExistingKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existing-key-http";

        // Act
        var result = await _cacheClient.GetAsync(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetWithTTL_ItemExpiresCorrectly()
    {
        // Arrange
        var key = "ttl-key-http";
        var value = "ttl-value-http";
        var ttl = 2; // 2 seconds

        // Act
        var setResult = await _cacheClient.SetAsync(key, value, ttl);
        var getResultBefore = await _cacheClient.GetAsync(key);
        
        await Task.Delay(2100); // Wait for expiration
        var getResultAfter = await _cacheClient.GetAsync(key);

        // Assert
        Assert.True(setResult);
        Assert.NotNull(getResultBefore);
        Assert.Null(getResultAfter);
    }

    [Fact]
    public async Task Remove_ExistingKey_Success()
    {
        // Arrange
        var key = "remove-key-http";
        var value = "remove-value-http";
        await _cacheClient.SetAsync(key, value);

        // Act
        var removeResult = await _cacheClient.RemoveAsync(key);
        var getResult = await _cacheClient.GetAsync(key);

        // Assert
        Assert.True(removeResult);
        Assert.Null(getResult);
    }

    [Fact]
    public async Task Remove_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existing-remove-key-http";

        // Act
        var result = await _cacheClient.RemoveAsync(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Exists_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "exists-key-http";
        var value = "exists-value-http";
        await _cacheClient.SetAsync(key, value);

        // Act
        var result = await _cacheClient.ExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Exists_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existing-exists-key-http";

        // Act
        var result = await _cacheClient.ExistsAsync(key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Flush_ClearsAllItems()
    {
        // Arrange
        await _cacheClient.SetAsync("flush-key1", "value1");
        await _cacheClient.SetAsync("flush-key2", "value2");

        // Act
        var flushResult = await _cacheClient.FlushAsync();
        var key1Result = await _cacheClient.GetAsync("flush-key1");
        var key2Result = await _cacheClient.GetAsync("flush-key2");

        // Assert
        Assert.True(flushResult);
        Assert.Null(key1Result);
        Assert.Null(key2Result);
    }

    [Fact]
    public async Task GetStats_ReturnsValidStatistics()
    {
        // Arrange
        await _cacheClient.SetAsync("stats-key1", "value1");
        await _cacheClient.SetAsync("stats-key2", "value2");
        await _cacheClient.GetAsync("stats-key1"); // Hit
        await _cacheClient.GetAsync("non-existing"); // Miss

        // Act
        var stats = await _cacheClient.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalKeys >= 2);
        Assert.True(stats.Hits >= 1);
        Assert.True(stats.Misses >= 1);
        Assert.True(stats.UptimeSeconds >= 0);
        Assert.True(stats.MemoryUsageBytes > 0);
    }

    [Fact]
    public async Task GetAll_ReturnsAllItems()
    {
        // Arrange
        var key1 = "getall-key1";
        var key2 = "getall-key2";
        await _cacheClient.SetAsync(key1, "value1");
        await _cacheClient.SetAsync(key2, "value2");

        // Act
        var result = await _cacheClient.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
        Assert.True(result.ContainsKey(key1) || result.Values.Any(v => v.Key == key1));
        Assert.True(result.ContainsKey(key2) || result.Values.Any(v => v.Key == key2));
    }

    [Fact]
    public async Task Search_ValidPattern_ReturnsMatchingItems()
    {
        // Arrange
        await _cacheClient.SetAsync("search:user:1", "John");
        await _cacheClient.SetAsync("search:user:2", "Jane");
        await _cacheClient.SetAsync("search:product:1", "Laptop");

        // Act
        var result = await _cacheClient.SearchAsync("search:user:.*");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
        Assert.True(result.Keys.Any(k => k.Contains("search:user:1")));
        Assert.True(result.Keys.Any(k => k.Contains("search:user:2")));
        Assert.False(result.Keys.Any(k => k.Contains("search:product:1")));
    }

    [Fact]
    public async Task ConcurrentOperations_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        var keyCount = 50;

        // Act - Concurrent sets
        for (int i = 0; i < keyCount; i++)
        {
            var key = $"concurrent-key-{i}";
            var value = $"concurrent-value-{i}";
            tasks.Add(_cacheClient.SetAsync(key, value));
        }

        var setResults = await Task.WhenAll(tasks);
        tasks.Clear();

        // Act - Concurrent gets
        var getTasks = new List<Task<bool>>();
        for (int i = 0; i < keyCount; i++)
        {
            var key = $"concurrent-key-{i}";
            getTasks.Add(Task.Run(async () => await _cacheClient.GetAsync(key) != null));
        }

        var getResults = await Task.WhenAll(getTasks);

        // Assert
        Assert.All(setResults, result => Assert.True(result));
        Assert.All(getResults, result => Assert.True(result));
    }
}
