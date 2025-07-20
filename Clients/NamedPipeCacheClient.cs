using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using SelfHostedCacheService.Models;

namespace SelfHostedCacheService.Clients;

/// <summary>
/// High-performance Named Pipe client for cache operations (Windows only)
/// </summary>
public class NamedPipeCacheClient : IDisposable
{
    private readonly string _pipeName;
    private readonly int _timeoutMs;
    private readonly ILogger<NamedPipeCacheClient>? _logger;
    private bool _disposed = false;

    public NamedPipeCacheClient(string pipeName = "cachepipe", int timeoutMs = 5000, ILogger<NamedPipeCacheClient>? logger = null)
    {
        _pipeName = pipeName;
        _timeoutMs = timeoutMs;
        _logger = logger;
    }

    /// <summary>
    /// Sends a raw JSON request and returns the response
    /// </summary>
    public async Task<string> SendRequestAsync(string requestJson)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Named Pipes are only supported on Windows");
        }

        using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
        
        try
        {
            await pipeClient.ConnectAsync(_timeoutMs);
            
            using var writer = new StreamWriter(pipeClient, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipeClient, Encoding.UTF8, leaveOpen: true);

            await writer.WriteLineAsync(requestJson);
            var response = await reader.ReadLineAsync();

            _logger?.LogDebug("Named Pipe request: {Request}, response: {Response}", requestJson, response);
            
            return response ?? string.Empty;
        }
        catch (TimeoutException)
        {
            _logger?.LogError("Named Pipe connection timeout after {Timeout}ms", _timeoutMs);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Named Pipe communication error");
            throw;
        }
    }

    /// <summary>
    /// Gets a cache item by key
    /// </summary>
    public async Task<CacheItemResponse?> GetAsync(string key)
    {
        var request = new CacheRequest { Action = "get", Key = key };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<CacheItemResponse>>(responseJson);
        return response?.Success == true ? response.Data : null;
    }

    /// <summary>
    /// Sets a cache item
    /// </summary>
    public async Task<bool> SetAsync(string key, object value, int? ttlSeconds = null)
    {
        var request = new CacheRequest { Action = "set", Key = key, Value = value, TtlSeconds = ttlSeconds };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<bool>>(responseJson);
        return response?.Success == true;
    }

    /// <summary>
    /// Removes a cache item by key
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        var request = new CacheRequest { Action = "remove", Key = key };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<bool>>(responseJson);
        return response?.Success == true;
    }

    /// <summary>
    /// Checks if a key exists
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        var request = new CacheRequest { Action = "exists", Key = key };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<bool>>(responseJson);
        return response?.Success == true && response.Data == true;
    }

    /// <summary>
    /// Flushes all cache items
    /// </summary>
    public async Task<bool> FlushAsync()
    {
        var request = new CacheRequest { Action = "flush" };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<bool>>(responseJson);
        return response?.Success == true;
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public async Task<CacheStats?> GetStatsAsync()
    {
        var request = new CacheRequest { Action = "stats" };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<CacheStats>>(responseJson);
        return response?.Success == true ? response.Data : null;
    }

    /// <summary>
    /// Gets all cache items
    /// </summary>
    public async Task<Dictionary<string, CacheItemResponse>?> GetAllAsync()
    {
        var request = new CacheRequest { Action = "all" };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<Dictionary<string, CacheItemResponse>>>(responseJson);
        return response?.Success == true ? response.Data : null;
    }

    /// <summary>
    /// Searches cache items by regex pattern
    /// </summary>
    public async Task<Dictionary<string, CacheItemResponse>?> SearchAsync(string pattern)
    {
        var request = new CacheRequest { Action = "search", Pattern = pattern };
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await SendRequestAsync(requestJson);

        var response = JsonSerializer.Deserialize<CacheResponse<Dictionary<string, CacheItemResponse>>>(responseJson);
        return response?.Success == true ? response.Data : null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
