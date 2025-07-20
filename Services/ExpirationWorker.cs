namespace SelfHostedCacheService.Services;

/// <summary>
/// Background service that periodically removes expired cache items
/// </summary>
public class ExpirationWorker : BackgroundService
{
    private readonly CacheManager _cacheManager;
    private readonly ILogger<ExpirationWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _scanIntervalSeconds;

    public ExpirationWorker(
        CacheManager cacheManager, 
        ILogger<ExpirationWorker> logger,
        IConfiguration configuration)
    {
        _cacheManager = cacheManager;
        _logger = logger;
        _configuration = configuration;
        _scanIntervalSeconds = _configuration.GetValue<int>("CacheSettings:ExpirationScanIntervalSeconds", 60);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expiration worker started with scan interval: {Interval}s", _scanIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removedCount = _cacheManager.CleanupExpiredItems();
                
                if (removedCount > 0)
                {
                    _logger.LogDebug("Expiration cleanup removed {Count} expired items", removedCount);
                }

                await Task.Delay(TimeSpan.FromSeconds(_scanIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expiration cleanup");
                
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Expiration worker stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Expiration worker is stopping");
        await base.StopAsync(cancellationToken);
    }
}
