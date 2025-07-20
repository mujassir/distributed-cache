using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using CacheClientApp.Net48.Models;
using Newtonsoft.Json;

namespace CacheClientApp.Net48.Services
{
    /// <summary>
    /// Named Pipe client for cache operations (.NET Framework 4.8 compatible)
    /// </summary>
    public class NamedPipeCacheClient : IDisposable
    {
        private readonly string _pipeName;
        private readonly int _timeoutMs;
        private bool _disposed = false;

        public NamedPipeCacheClient()
        {
            _pipeName = ConfigurationManager.AppSettings["NamedPipeName"] ?? "cachepipe";
            _timeoutMs = int.Parse(ConfigurationManager.AppSettings["RequestTimeoutMs"] ?? "5000");
        }

        public NamedPipeCacheClient(string pipeName, int timeoutMs = 5000)
        {
            _pipeName = pipeName ?? "cachepipe";
            _timeoutMs = timeoutMs;
        }

        /// <summary>
        /// Sends a raw JSON request and returns the response
        /// </summary>
        public async Task<string> SendRequestAsync(string requestJson)
        {
            // Check if running on Windows
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new PlatformNotSupportedException("Named Pipes are only supported on Windows");
            }

            return await Task.Run(() =>
            {
                using (var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut))
                {
                    try
                    {
                        pipeClient.Connect(_timeoutMs);
                        
                        using (var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true })
                        using (var reader = new StreamReader(pipeClient, Encoding.UTF8))
                        {
                            writer.WriteLine(requestJson);
                            return reader.ReadLine();
                        }
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine($"Named Pipe connection timeout after {_timeoutMs}ms");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Named Pipe communication error: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Gets a cache item by key
        /// </summary>
        public async Task<CacheItemResponse> GetAsync(string key)
        {
            var request = new NamedPipeRequest { Action = "get", Key = key };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return null;

            var response = JsonConvert.DeserializeObject<CacheResponse<CacheItemResponse>>(responseJson);
            return response?.Success == true ? response.Data : null;
        }

        /// <summary>
        /// Sets a cache item
        /// </summary>
        public async Task<bool> SetAsync(string key, object value, int? ttlSeconds = null)
        {
            var request = new NamedPipeRequest { Action = "set", Key = key, Value = value, TtlSeconds = ttlSeconds };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return false;

            var response = JsonConvert.DeserializeObject<CacheResponse<bool>>(responseJson);
            return response?.Success == true;
        }

        /// <summary>
        /// Removes a cache item by key
        /// </summary>
        public async Task<bool> RemoveAsync(string key)
        {
            var request = new NamedPipeRequest { Action = "remove", Key = key };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return false;

            var response = JsonConvert.DeserializeObject<CacheResponse<bool>>(responseJson);
            return response?.Success == true;
        }

        /// <summary>
        /// Checks if a key exists
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            var request = new NamedPipeRequest { Action = "exists", Key = key };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return false;

            var response = JsonConvert.DeserializeObject<CacheResponse<bool>>(responseJson);
            return response?.Success == true && response.Data;
        }

        /// <summary>
        /// Flushes all cache items
        /// </summary>
        public async Task<bool> FlushAsync()
        {
            var request = new NamedPipeRequest { Action = "flush" };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return false;

            var response = JsonConvert.DeserializeObject<CacheResponse<bool>>(responseJson);
            return response?.Success == true;
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public async Task<CacheStats> GetStatsAsync()
        {
            var request = new NamedPipeRequest { Action = "stats" };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return null;

            var response = JsonConvert.DeserializeObject<CacheResponse<CacheStats>>(responseJson);
            return response?.Success == true ? response.Data : null;
        }

        /// <summary>
        /// Gets all cache items
        /// </summary>
        public async Task<Dictionary<string, CacheItemResponse>> GetAllAsync()
        {
            var request = new NamedPipeRequest { Action = "all" };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return null;

            var response = JsonConvert.DeserializeObject<CacheResponse<Dictionary<string, CacheItemResponse>>>(responseJson);
            return response?.Success == true ? response.Data : null;
        }

        /// <summary>
        /// Searches cache items by regex pattern
        /// </summary>
        public async Task<Dictionary<string, CacheItemResponse>> SearchAsync(string pattern)
        {
            var request = new NamedPipeRequest { Action = "search", Pattern = pattern };
            var requestJson = JsonConvert.SerializeObject(request);
            var responseJson = await SendRequestAsync(requestJson);

            if (string.IsNullOrEmpty(responseJson))
                return null;

            var response = JsonConvert.DeserializeObject<CacheResponse<Dictionary<string, CacheItemResponse>>>(responseJson);
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
}
