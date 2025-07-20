using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using SelfHostedCacheService.Models;

namespace SelfHostedCacheService.Services;

/// <summary>
/// Named Pipe server for high-performance IPC communication
/// </summary>
public class NamedPipeServer : BackgroundService
{
    private readonly CacheManager _cacheManager;
    private readonly ILogger<NamedPipeServer> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _pipeName;
    private readonly bool _isEnabled;

    public NamedPipeServer(
        CacheManager cacheManager,
        ILogger<NamedPipeServer> logger,
        IConfiguration configuration)
    {
        _cacheManager = cacheManager;
        _logger = logger;
        _configuration = configuration;
        _pipeName = _configuration.GetValue<string>("CacheSettings:NamedPipeName", "cachepipe")!;
        _isEnabled = _configuration.GetValue<bool>("CacheSettings:EnableNamedPipes", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Named Pipe server is disabled");
            return;
        }

        // Check if running on Windows (Named Pipes are Windows-specific)
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Named Pipes are only supported on Windows. Skipping Named Pipe server.");
            return;
        }

        _logger.LogInformation("Named Pipe server starting on pipe: {PipeName}", _pipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogDebug("Waiting for client connection on pipe: {PipeName}", _pipeName);

                await pipeServer.WaitForConnectionAsync(stoppingToken);
                _logger.LogDebug("Client connected to Named Pipe");

                // Handle the client in a separate task to allow multiple concurrent connections
                _ = Task.Run(async () => await HandleClientAsync(pipeServer), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Named Pipe server");
                await Task.Delay(1000, stoppingToken); // Brief delay before retrying
            }
        }

        _logger.LogInformation("Named Pipe server stopped");
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipeServer)
    {
        try
        {
            using var reader = new StreamReader(pipeServer, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(pipeServer, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            while (pipeServer.IsConnected)
            {
                var requestJson = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestJson))
                    break;

                _logger.LogDebug("Named Pipe received: {Request}", requestJson);

                var response = await ProcessRequestAsync(requestJson);
                await writer.WriteLineAsync(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Named Pipe client");
        }
        finally
        {
            try
            {
                pipeServer.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disconnecting Named Pipe client");
            }
        }
    }

    private async Task<string> ProcessRequestAsync(string requestJson)
    {
        try
        {
            var request = JsonSerializer.Deserialize<CacheRequest>(requestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Invalid request format"));
            }

            return request.Action?.ToLowerInvariant() switch
            {
                "get" => await HandleGetAsync(request),
                "set" => await HandleSetAsync(request),
                "remove" => await HandleRemoveAsync(request),
                "exists" => await HandleExistsAsync(request),
                "flush" => await HandleFlushAsync(),
                "stats" => await HandleStatsAsync(),
                "all" => await HandleGetAllAsync(),
                "search" => await HandleSearchAsync(request),
                _ => JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse($"Unknown action: {request.Action}"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Named Pipe request: {Request}", requestJson);
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Internal server error"));
        }
    }

    private async Task<string> HandleGetAsync(CacheRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Key is required"));
        }

        var item = _cacheManager.Get(request.Key);
        if (item == null)
        {
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Key not found"));
        }

        var response = new CacheItemResponse
        {
            Key = item.Key,
            Value = item.Value,
            RemainingTtlSeconds = item.RemainingTtlSeconds,
            CreatedAt = item.CreatedAt
        };

        return JsonSerializer.Serialize(CacheResponse<CacheItemResponse>.SuccessResponse(response));
    }

    private async Task<string> HandleSetAsync(CacheRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Key is required"));
        }

        var success = _cacheManager.Set(request.Key, request.Value, request.TtlSeconds);
        return JsonSerializer.Serialize(success 
            ? CacheResponse<bool>.SuccessResponse(true, "Item added successfully")
            : CacheResponse<bool>.ErrorResponse("Failed to add item"));
    }

    private async Task<string> HandleRemoveAsync(CacheRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Key is required"));
        }

        var success = _cacheManager.Remove(request.Key);
        return JsonSerializer.Serialize(success
            ? CacheResponse<bool>.SuccessResponse(true, "Item removed successfully")
            : CacheResponse<bool>.ErrorResponse("Key not found"));
    }

    private async Task<string> HandleExistsAsync(CacheRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Key is required"));
        }

        var exists = _cacheManager.Exists(request.Key);
        return JsonSerializer.Serialize(CacheResponse<bool>.SuccessResponse(exists));
    }

    private async Task<string> HandleFlushAsync()
    {
        _cacheManager.Flush();
        return JsonSerializer.Serialize(CacheResponse<bool>.SuccessResponse(true, "Cache flushed successfully"));
    }

    private async Task<string> HandleStatsAsync()
    {
        var stats = _cacheManager.GetStats();
        return JsonSerializer.Serialize(CacheResponse<CacheStats>.SuccessResponse(stats));
    }

    private async Task<string> HandleGetAllAsync()
    {
        var items = _cacheManager.GetAll();
        var response = items.ToDictionary(
            kvp => kvp.Key,
            kvp => new CacheItemResponse
            {
                Key = kvp.Value.Key,
                Value = kvp.Value.Value,
                RemainingTtlSeconds = kvp.Value.RemainingTtlSeconds,
                CreatedAt = kvp.Value.CreatedAt
            });

        return JsonSerializer.Serialize(CacheResponse<Dictionary<string, CacheItemResponse>>.SuccessResponse(response));
    }

    private async Task<string> HandleSearchAsync(CacheRequest request)
    {
        if (string.IsNullOrEmpty(request.Pattern))
        {
            return JsonSerializer.Serialize(CacheResponse<object>.ErrorResponse("Pattern is required"));
        }

        var items = _cacheManager.Search(request.Pattern);
        var response = items.ToDictionary(
            kvp => kvp.Key,
            kvp => new CacheItemResponse
            {
                Key = kvp.Value.Key,
                Value = kvp.Value.Value,
                RemainingTtlSeconds = kvp.Value.RemainingTtlSeconds,
                CreatedAt = kvp.Value.CreatedAt
            });

        return JsonSerializer.Serialize(CacheResponse<Dictionary<string, CacheItemResponse>>.SuccessResponse(response));
    }
}
