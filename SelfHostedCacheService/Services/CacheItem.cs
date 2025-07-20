using System;

namespace SelfHostedCacheService.Services
{
    public class CacheItem
    {
        public object Value { get; set; }
        public DateTime? Expiry { get; set; }
        public int HitCount { get; set; }
    }
}