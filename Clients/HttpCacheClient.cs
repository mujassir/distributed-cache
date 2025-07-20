using System.Text;
using System.Text.Json;
using SelfHostedCacheService.Models;

namespace SelfHostedCacheService.Clients;

/// <summary>
/// HTTP client for cache operations (fallback mode)
/// </summary>
public class HttpCacheClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<HttpCacheClient>? _logger;
    private bool _disposed = false;

    public HttpCacheClient(string baseUrl = "http://localhost:5000", string apiKey = "dev-key-12345", ILogger<HttpCacheClient>? logger = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public HttpCacheClient(HttpClient httpClient, string baseUrl = "http://localhost:5000", ILogger<HttpCacheClient>? logger = null)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
    }

    /// <summary>
    /// Gets a cache item by key
    /// </summary>
    public async Task<CacheItemResponse?> GetAsync(string key)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/cache/{Uri.EscapeDataString(key)}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<CacheItemResponse>>(json);

            _logger?.LogDebug("HTTP GET {Key}: {Success}", key, cacheResponse?.Success);
            return cacheResponse?.Success == true ? cacheResponse.Data : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting cache item {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Sets a cache item
    /// </summary>
    public async Task<bool> SetAsync(string key, object value, int? ttlSeconds = null)
    {
        try
        {
            var request = new AddCacheRequest { Key = key, Value = value, TtlSeconds = ttlSeconds };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/cache", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<bool>>(responseJson);

            _logger?.LogDebug("HTTP SET {Key}: {Success}", key, cacheResponse?.Success);
            return cacheResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting cache item {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Removes a cache item by key
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/cache/{Uri.EscapeDataString(key)}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<bool>>(json);

            _logger?.LogDebug("HTTP DELETE {Key}: {Success}", key, cacheResponse?.Success);
            return cacheResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing cache item {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Checks if a key exists
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/cache/exists/{Uri.EscapeDataString(key)}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<bool>>(json);

            _logger?.LogDebug("HTTP EXISTS {Key}: {Exists}", key, cacheResponse?.Data);
            return cacheResponse?.Success == true && cacheResponse.Data == true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking key existence {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Flushes all cache items
    /// </summary>
    public async Task<bool> FlushAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/cache/flush", null);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<bool>>(json);

            _logger?.LogDebug("HTTP FLUSH: {Success}", cacheResponse?.Success);
            return cacheResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error flushing cache");
            throw;
        }
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public async Task<CacheStats?> GetStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/cache/stats");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<CacheStats>>(json);

            _logger?.LogDebug("HTTP STATS: {Success}", cacheResponse?.Success);
            return cacheResponse?.Success == true ? cacheResponse.Data : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting cache stats");
            throw;
        }
    }

    /// <summary>
    /// Gets all cache items
    /// </summary>
    public async Task<Dictionary<string, CacheItemResponse>?> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/cache/all");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<Dictionary<string, CacheItemResponse>>>(json);

            _logger?.LogDebug("HTTP GET ALL: {Success}, Count: {Count}", cacheResponse?.Success, cacheResponse?.Data?.Count);
            return cacheResponse?.Success == true ? cacheResponse.Data : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all cache items");
            throw;
        }
    }

    /// <summary>
    /// Searches cache items by regex pattern
    /// </summary>
    public async Task<Dictionary<string, CacheItemResponse>?> SearchAsync(string pattern)
    {
        try
        {
            var encodedPattern = Uri.EscapeDataString(pattern);
            var response = await _httpClient.GetAsync($"{_baseUrl}/cache/search?pattern={encodedPattern}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var cacheResponse = JsonSerializer.Deserialize<CacheResponse<Dictionary<string, CacheItemResponse>>>(json);

            _logger?.LogDebug("HTTP SEARCH '{Pattern}': {Success}, Count: {Count}", pattern, cacheResponse?.Success, cacheResponse?.Data?.Count);
            return cacheResponse?.Success == true ? cacheResponse.Data : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching cache with pattern {Pattern}", pattern);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
