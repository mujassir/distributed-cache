using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CacheClientApp.Net48.Models;
using Newtonsoft.Json;

namespace CacheClientApp.Net48.Services
{
    /// <summary>
    /// HTTP client for cache operations (.NET Framework 4.8 compatible)
    /// </summary>
    public class HttpCacheClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed = false;

        public HttpCacheClient()
        {
            _baseUrl = ConfigurationManager.AppSettings["CacheServiceUrl"] ?? "http://localhost:5000";
            var apiKey = ConfigurationManager.AppSettings["ApiKey"] ?? "dev-key-12345";
            var timeoutMs = int.Parse(ConfigurationManager.AppSettings["RequestTimeoutMs"] ?? "5000");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        }

        public HttpCacheClient(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? "http://localhost:5000";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Gets a cache item by key
        /// </summary>
        public async Task<CacheItemResponse> GetAsync(string key)
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
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<CacheItemResponse>>(json);

                return cacheResponse?.Success == true ? cacheResponse.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cache item {key}: {ex.Message}");
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
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/cache", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<bool>>(responseJson);

                return cacheResponse?.Success == true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting cache item {key}: {ex.Message}");
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
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<bool>>(json);

                return cacheResponse?.Success == true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing cache item {key}: {ex.Message}");
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
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<bool>>(json);

                return cacheResponse?.Success == true && cacheResponse.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking key existence {key}: {ex.Message}");
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
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<bool>>(json);

                return cacheResponse?.Success == true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error flushing cache: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public async Task<CacheStats> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/cache/stats");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<CacheStats>>(json);

                return cacheResponse?.Success == true ? cacheResponse.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cache stats: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all cache items
        /// </summary>
        public async Task<Dictionary<string, CacheItemResponse>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/cache/all");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<Dictionary<string, CacheItemResponse>>>(json);

                return cacheResponse?.Success == true ? cacheResponse.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all cache items: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Searches cache items by regex pattern
        /// </summary>
        public async Task<Dictionary<string, CacheItemResponse>> SearchAsync(string pattern)
        {
            try
            {
                var encodedPattern = HttpUtility.UrlEncode(pattern);
                var response = await _httpClient.GetAsync($"{_baseUrl}/cache/search?pattern={encodedPattern}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var cacheResponse = JsonConvert.DeserializeObject<CacheResponse<Dictionary<string, CacheItemResponse>>>(json);

                return cacheResponse?.Success == true ? cacheResponse.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching cache with pattern {pattern}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks service health
        /// </summary>
        public async Task<HealthResponse> GetHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<HealthResponse>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking service health: {ex.Message}");
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
}
