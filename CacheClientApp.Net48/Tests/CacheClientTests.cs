using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CacheClientApp.Net48.Services;

namespace CacheClientApp.Net48.Tests
{
    /// <summary>
    /// Test suite for cache client functionality
    /// </summary>
    public class CacheClientTests
    {
        private readonly HttpCacheClient _httpClient;
        private readonly NamedPipeCacheClient _namedPipeClient;

        public CacheClientTests()
        {
            _httpClient = new HttpCacheClient();
            _namedPipeClient = new NamedPipeCacheClient();
        }

        /// <summary>
        /// Tests HTTP client functionality
        /// </summary>
        public async Task RunHttpClientTestsAsync()
        {
            Console.WriteLine("=== HTTP Client Tests (.NET Framework 4.8) ===");
            Console.WriteLine();

            try
            {
                // Test 1: Health Check
                Console.WriteLine("1. Testing Health Check...");
                var health = await _httpClient.GetHealthAsync();
                Console.WriteLine($"   Status: {health?.Status}");
                Console.WriteLine($"   Mode: {health?.Mode}");
                Console.WriteLine($"   Timestamp: {health?.Timestamp}");
                Console.WriteLine();

                // Test 2: Set and Get
                Console.WriteLine("2. Testing Set and Get operations...");
                var testKey = "net48:test:" + DateTime.Now.Ticks;
                var testValue = new
                {
                    Message = "Hello from .NET Framework 4.8!",
                    Timestamp = DateTime.Now,
                    Version = "4.8",
                    TestId = Guid.NewGuid()
                };

                var setResult = await _httpClient.SetAsync(testKey, testValue, 300);
                Console.WriteLine($"   Set result: {setResult}");

                var getResult = await _httpClient.GetAsync(testKey);
                if (getResult != null)
                {
                    Console.WriteLine($"   Retrieved key: {getResult.Key}");
                    Console.WriteLine($"   Retrieved value: {getResult.Value}");
                    Console.WriteLine($"   TTL remaining: {getResult.RemainingTtlSeconds}s");
                }
                Console.WriteLine();

                // Test 3: Exists
                Console.WriteLine("3. Testing Exists operation...");
                var exists = await _httpClient.ExistsAsync(testKey);
                Console.WriteLine($"   Key exists: {exists}");
                Console.WriteLine();

                // Test 4: Multiple items for search
                Console.WriteLine("4. Setting up multiple items for search test...");
                await _httpClient.SetAsync("net48:user:1", new { Name = "John", Age = 30 });
                await _httpClient.SetAsync("net48:user:2", new { Name = "Jane", Age = 25 });
                await _httpClient.SetAsync("net48:product:1", new { Name = "Laptop", Price = 999.99 });
                Console.WriteLine("   Multiple items set successfully");
                Console.WriteLine();

                // Test 5: Search
                Console.WriteLine("5. Testing Search operation...");
                var searchResults = await _httpClient.SearchAsync("net48:user:.*");
                if (searchResults != null)
                {
                    Console.WriteLine($"   Found {searchResults.Count} matching items:");
                    foreach (var item in searchResults)
                    {
                        Console.WriteLine($"     {item.Key}: {item.Value.Value}");
                    }
                }
                Console.WriteLine();

                // Test 6: Get All
                Console.WriteLine("6. Testing Get All operation...");
                var allItems = await _httpClient.GetAllAsync();
                if (allItems != null)
                {
                    Console.WriteLine($"   Total items in cache: {allItems.Count}");
                    var count = 0;
                    foreach (var item in allItems)
                    {
                        if (count < 5) // Show first 5 items
                        {
                            Console.WriteLine($"     {item.Key}: {item.Value.Value}");
                        }
                        count++;
                    }
                    if (allItems.Count > 5)
                    {
                        Console.WriteLine($"     ... and {allItems.Count - 5} more items");
                    }
                }
                Console.WriteLine();

                // Test 7: Statistics
                Console.WriteLine("7. Testing Statistics...");
                var stats = await _httpClient.GetStatsAsync();
                if (stats != null)
                {
                    Console.WriteLine($"   Total keys: {stats.TotalKeys}");
                    Console.WriteLine($"   Cache hits: {stats.Hits}");
                    Console.WriteLine($"   Cache misses: {stats.Misses}");
                    Console.WriteLine($"   Hit rate: {stats.HitRate:P2}");
                    Console.WriteLine($"   Memory usage: {stats.MemoryUsageBytes / 1024 / 1024:F2} MB");
                    Console.WriteLine($"   Uptime: {stats.UptimeSeconds} seconds");
                }
                Console.WriteLine();

                // Test 8: Remove
                Console.WriteLine("8. Testing Remove operation...");
                var removeResult = await _httpClient.RemoveAsync(testKey);
                Console.WriteLine($"   Remove result: {removeResult}");

                var existsAfterRemove = await _httpClient.ExistsAsync(testKey);
                Console.WriteLine($"   Key exists after remove: {existsAfterRemove}");
                Console.WriteLine();

                // Test 9: Performance Test
                Console.WriteLine("9. Performance test (100 operations)...");
                var stopwatch = Stopwatch.StartNew();
                var tasks = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    var key = $"net48:perf:{i}";
                    var value = new { Id = i, Data = $"Performance test data {i}" };
                    tasks.Add(_httpClient.SetAsync(key, value));
                }

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                Console.WriteLine($"   100 SET operations completed in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"   Average: {stopwatch.ElapsedMilliseconds / 100.0:F2}ms per operation");
                Console.WriteLine($"   Throughput: {100.0 / stopwatch.Elapsed.TotalSeconds:F0} ops/sec");
                Console.WriteLine();

                Console.WriteLine("✅ HTTP Client Tests Completed Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTTP Client Test Failed: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tests Named Pipe client functionality (Windows only)
        /// </summary>
        public async Task RunNamedPipeClientTestsAsync()
        {
            Console.WriteLine("=== Named Pipe Client Tests (.NET Framework 4.8) ===");
            Console.WriteLine();

            try
            {
                // Check if running on Windows
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    Console.WriteLine("⚠️  Named Pipes are only supported on Windows. Skipping Named Pipe tests.");
                    return;
                }

                // Test 1: Set and Get
                Console.WriteLine("1. Testing Named Pipe Set and Get operations...");
                var testKey = "net48:pipe:test:" + DateTime.Now.Ticks;
                var testValue = new
                {
                    Message = "Hello from Named Pipes (.NET Framework 4.8)!",
                    Timestamp = DateTime.Now,
                    Protocol = "Named Pipes",
                    TestId = Guid.NewGuid()
                };

                var setResult = await _namedPipeClient.SetAsync(testKey, testValue, 300);
                Console.WriteLine($"   Set result: {setResult}");

                var getResult = await _namedPipeClient.GetAsync(testKey);
                if (getResult != null)
                {
                    Console.WriteLine($"   Retrieved key: {getResult.Key}");
                    Console.WriteLine($"   Retrieved value: {getResult.Value}");
                    Console.WriteLine($"   TTL remaining: {getResult.RemainingTtlSeconds}s");
                }
                Console.WriteLine();

                // Test 2: Statistics
                Console.WriteLine("2. Testing Named Pipe Statistics...");
                var stats = await _namedPipeClient.GetStatsAsync();
                if (stats != null)
                {
                    Console.WriteLine($"   Total keys: {stats.TotalKeys}");
                    Console.WriteLine($"   Hit rate: {stats.HitRate:P2}");
                    Console.WriteLine($"   Memory usage: {stats.MemoryUsageBytes / 1024 / 1024:F2} MB");
                }
                Console.WriteLine();

                // Test 3: Performance Test
                Console.WriteLine("3. Named Pipe Performance test (50 operations)...");
                var stopwatch = Stopwatch.StartNew();
                var tasks = new List<Task>();

                for (int i = 0; i < 50; i++)
                {
                    var key = $"net48:pipe:perf:{i}";
                    var value = new { Id = i, Data = $"Named Pipe performance test {i}" };
                    tasks.Add(_namedPipeClient.SetAsync(key, value));
                }

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                Console.WriteLine($"   50 SET operations completed in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"   Average: {stopwatch.ElapsedMilliseconds / 50.0:F2}ms per operation");
                Console.WriteLine($"   Throughput: {50.0 / stopwatch.Elapsed.TotalSeconds:F0} ops/sec");
                Console.WriteLine();

                Console.WriteLine("✅ Named Pipe Client Tests Completed Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Named Pipe Client Test Failed: {ex.Message}");
                if (ex.Message.Contains("No process is on the other end of the pipe"))
                {
                    Console.WriteLine("💡 Make sure the cache service is running in Named Pipe mode:");
                    Console.WriteLine("   dotnet run -- --pipe-mode");
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _namedPipeClient?.Dispose();
        }
    }
}
