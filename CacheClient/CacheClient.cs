using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;

namespace CacheClient
{
    public class CacheClient
    {
        private static readonly HttpClient _httpClient;
        public string BaseUrl { get; set; } = "http://localhost:5000";

        static CacheClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", "your-default-api-key");
        }

        public async Task<string> GetAsync(string key)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/cache/{key}");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }

        public async Task<bool> AddAsync(string key, object value, int ttlSeconds = 0)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new { key, value, ttl = ttlSeconds }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/cache", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/cache/{key}");
            return response.IsSuccessStatusCode;
        }

        public async Task<string> SearchAsync(string pattern)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/cache/search?pattern={HttpUtility.UrlEncode(pattern)}");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }
    }
}