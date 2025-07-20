using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SelfHostedCacheService.Services
{
    public class ExpirationWorker : BackgroundService
    {
        private readonly CacheManager _cacheManager;
        private readonly TimeSpan _scanInterval;

        public ExpirationWorker(CacheManager cacheManager, TimeSpan? scanInterval = null)
        {
            _cacheManager = cacheManager;
            _scanInterval = scanInterval ?? TimeSpan.FromSeconds(30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var key in _cacheManager.Keys)
                {
                    var item = _cacheManager.Get(key);
                    if (item == null)
                    {
                        _cacheManager.Remove(key);
                    }
                }
                await Task.Delay(_scanInterval, stoppingToken);
            }
        }
    }
}