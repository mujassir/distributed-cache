using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SelfHostedCacheService.Services
{
    public class CacheManager
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private int _hits = 0;
        private int _misses = 0;

        public int Hits => _hits;
        public int Misses => _misses;
        public int KeyCount => _cache.Count;

        public bool Add(string key, object value, int ttlSeconds = 0)
        {
            var expiry = ttlSeconds > 0 ? DateTime.UtcNow.AddSeconds(ttlSeconds) : (DateTime?)null;
            var item = new CacheItem { Value = value, Expiry = expiry, HitCount = 0 };
            _cache[key] = item;
            return true;
        }

        public object Get(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.Expiry.HasValue && item.Expiry.Value < DateTime.UtcNow)
                {
                    _cache.TryRemove(key, out _);
                    _misses++;
                    return null;
                }
                item.HitCount++;
                _hits++;
                return item.Value;
            }
            _misses++;
            return null;
        }

        public bool Remove(string key) => _cache.TryRemove(key, out _);

        public bool Exists(string key) => _cache.ContainsKey(key);

        public void Flush() => _cache.Clear();

        public IEnumerable<string> Keys => _cache.Keys;

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            foreach (var kvp in _cache)
            {
                if (!kvp.Value.Expiry.HasValue || kvp.Value.Expiry.Value >= DateTime.UtcNow)
                    yield return new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value);
            }
        }

        public IEnumerable<KeyValuePair<string, object>> Search(string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            foreach (var kvp in _cache)
            {
                if (regex.IsMatch(kvp.Key) && (!kvp.Value.Expiry.HasValue || kvp.Value.Expiry.Value >= DateTime.UtcNow))
                    yield return new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value);
            }
        }
    }
}