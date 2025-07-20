using System.Text.Json;
using SelfHostedCacheService.Models;

namespace SelfHostedCacheService.Middleware;

/// <summary>
/// Middleware for API key authentication
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly HashSet<string> _allowedApiKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
        
        var apiKeys = _configuration.GetSection("AllowedApiKeys").Get<string[]>() ?? Array.Empty<string>();
        _allowedApiKeys = new HashSet<string>(apiKeys, StringComparer.OrdinalIgnoreCase);
        
        _logger.LogInformation("API Key middleware initialized with {Count} allowed keys", _allowedApiKeys.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health checks and swagger
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path != null && (path.StartsWith("/health") || 
                            path.StartsWith("/swagger") || 
                            path.StartsWith("/api-docs") ||
                            path == "/" ||
                            path == "/favicon.ico"))
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var apiKeyValues))
        {
            _logger.LogWarning("Missing API key for request: {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "Missing API key");
            return;
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey) || !_allowedApiKeys.Contains(apiKey))
        {
            _logger.LogWarning("Invalid API key for request: {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "Invalid API key");
            return;
        }

        _logger.LogDebug("API key validated for request: {Path}", context.Request.Path);
        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var response = CacheResponse<object>.ErrorResponse(message);
        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
