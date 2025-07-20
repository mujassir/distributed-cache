using Microsoft.AspNetCore.Mvc;
using SelfHostedCacheService.Models;
using SelfHostedCacheService.Services;

namespace SelfHostedCacheService.Controllers;

/// <summary>
/// HTTP REST API controller for cache operations
/// </summary>
[ApiController]
[Route("cache")]
[Produces("application/json")]
public class CacheController : ControllerBase
{
    private readonly CacheManager _cacheManager;
    private readonly ILogger<CacheController> _logger;

    public CacheController(CacheManager cacheManager, ILogger<CacheController> logger)
    {
        _cacheManager = cacheManager;
        _logger = logger;
    }

    /// <summary>
    /// Add or update a cache item
    /// </summary>
    [HttpPost]
    public ActionResult<CacheResponse<bool>> AddItem([FromBody] AddCacheRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return BadRequest(CacheResponse<bool>.ErrorResponse("Key is required"));
        }

        try
        {
            var success = _cacheManager.Set(request.Key, request.Value, request.TtlSeconds);
            
            if (success)
            {
                _logger.LogDebug("HTTP API: Added cache item {Key}", request.Key);
                return Ok(CacheResponse<bool>.SuccessResponse(true, "Item added successfully"));
            }
            
            return StatusCode(500, CacheResponse<bool>.ErrorResponse("Failed to add item"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cache item {Key}", request.Key);
            return StatusCode(500, CacheResponse<bool>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get a cache item by key
    /// </summary>
    [HttpGet("{key}")]
    public ActionResult<CacheResponse<CacheItemResponse>> GetItem(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return BadRequest(CacheResponse<CacheItemResponse>.ErrorResponse("Key is required"));
        }

        try
        {
            var item = _cacheManager.Get(key);
            
            if (item == null)
            {
                _logger.LogDebug("HTTP API: Cache miss for key {Key}", key);
                return NotFound(CacheResponse<CacheItemResponse>.ErrorResponse("Key not found"));
            }

            var response = new CacheItemResponse
            {
                Key = item.Key,
                Value = item.Value,
                RemainingTtlSeconds = item.RemainingTtlSeconds,
                CreatedAt = item.CreatedAt
            };

            _logger.LogDebug("HTTP API: Cache hit for key {Key}", key);
            return Ok(CacheResponse<CacheItemResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache item {Key}", key);
            return StatusCode(500, CacheResponse<CacheItemResponse>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Remove a cache item by key
    /// </summary>
    [HttpDelete("{key}")]
    public ActionResult<CacheResponse<bool>> RemoveItem(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return BadRequest(CacheResponse<bool>.ErrorResponse("Key is required"));
        }

        try
        {
            var success = _cacheManager.Remove(key);
            
            if (success)
            {
                _logger.LogDebug("HTTP API: Removed cache item {Key}", key);
                return Ok(CacheResponse<bool>.SuccessResponse(true, "Item removed successfully"));
            }
            
            return NotFound(CacheResponse<bool>.ErrorResponse("Key not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache item {Key}", key);
            return StatusCode(500, CacheResponse<bool>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Check if a key exists
    /// </summary>
    [HttpGet("exists/{key}")]
    public ActionResult<CacheResponse<bool>> KeyExists(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return BadRequest(CacheResponse<bool>.ErrorResponse("Key is required"));
        }

        try
        {
            var exists = _cacheManager.Exists(key);
            _logger.LogDebug("HTTP API: Key exists check for {Key}: {Exists}", key, exists);
            return Ok(CacheResponse<bool>.SuccessResponse(exists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence {Key}", key);
            return StatusCode(500, CacheResponse<bool>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Clear all cache items
    /// </summary>
    [HttpPost("flush")]
    public ActionResult<CacheResponse<bool>> FlushCache()
    {
        try
        {
            _cacheManager.Flush();
            _logger.LogInformation("HTTP API: Cache flushed");
            return Ok(CacheResponse<bool>.SuccessResponse(true, "Cache flushed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
            return StatusCode(500, CacheResponse<bool>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("stats")]
    public ActionResult<CacheResponse<CacheStats>> GetStats()
    {
        try
        {
            var stats = _cacheManager.GetStats();
            return Ok(CacheResponse<CacheStats>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache stats");
            return StatusCode(500, CacheResponse<CacheStats>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get all cache items
    /// </summary>
    [HttpGet("all")]
    public ActionResult<CacheResponse<Dictionary<string, CacheItemResponse>>> GetAllItems()
    {
        try
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

            _logger.LogDebug("HTTP API: Retrieved all cache items, count: {Count}", response.Count);
            return Ok(CacheResponse<Dictionary<string, CacheItemResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all cache items");
            return StatusCode(500, CacheResponse<Dictionary<string, CacheItemResponse>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Search cache items by regex pattern
    /// </summary>
    [HttpGet("search")]
    public ActionResult<CacheResponse<Dictionary<string, CacheItemResponse>>> SearchItems([FromQuery] string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return BadRequest(CacheResponse<Dictionary<string, CacheItemResponse>>.ErrorResponse("Pattern is required"));
        }

        try
        {
            var items = _cacheManager.Search(pattern);
            var response = items.ToDictionary(
                kvp => kvp.Key,
                kvp => new CacheItemResponse
                {
                    Key = kvp.Value.Key,
                    Value = kvp.Value.Value,
                    RemainingTtlSeconds = kvp.Value.RemainingTtlSeconds,
                    CreatedAt = kvp.Value.CreatedAt
                });

            _logger.LogDebug("HTTP API: Search pattern '{Pattern}' found {Count} matches", pattern, response.Count);
            return Ok(CacheResponse<Dictionary<string, CacheItemResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cache with pattern {Pattern}", pattern);
            return StatusCode(500, CacheResponse<Dictionary<string, CacheItemResponse>>.ErrorResponse("Internal server error"));
        }
    }
}
