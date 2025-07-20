using System;
using System.Configuration;
using System.Threading.Tasks;
using CacheClientApp.Net48.Services;
using CacheClientApp.Net48.Tests;

namespace CacheClientApp.Net48
{
    /// <summary>
    /// Cache Service Client Application for .NET Framework 4.8
    /// Demonstrates integration with the self-hosted cache service
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Self-hosted Cache Service - .NET Framework 4.8 Client");
            Console.WriteLine("=====================================================");
            Console.WriteLine();

            // Display configuration
            var serviceUrl = ConfigurationManager.AppSettings["CacheServiceUrl"];
            var apiKey = ConfigurationManager.AppSettings["ApiKey"];
            var pipeName = ConfigurationManager.AppSettings["NamedPipeName"];

            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Service URL: {serviceUrl}");
            Console.WriteLine($"  API Key: {apiKey}");
            Console.WriteLine($"  Named Pipe: {pipeName}");
            Console.WriteLine($"  OS Platform: {Environment.OSVersion.Platform}");
            Console.WriteLine($"  .NET Version: {Environment.Version}");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var mode = args[0].ToLowerInvariant();

            try
            {
                switch (mode)
                {
                    case "http":
                    case "--http":
                        await RunHttpClientDemo();
                        break;

                    case "pipe":
                    case "namedpipe":
                    case "--pipe":
                        await RunNamedPipeClientDemo();
                        break;

                    case "test":
                    case "--test":
                        await RunAllTests();
                        break;

                    case "interactive":
                    case "--interactive":
                        await RunInteractiveMode();
                        break;

                    case "benchmark":
                    case "--benchmark":
                        await RunBenchmark();
                        break;

                    default:
                        Console.WriteLine($"Unknown mode: {mode}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Make sure the cache service is running:");
                Console.WriteLine("  For HTTP mode: dotnet run -- --http-mode");
                Console.WriteLine("  For Named Pipe mode: dotnet run -- --pipe-mode");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: CacheClientApp.Net48.exe <mode>");
            Console.WriteLine();
            Console.WriteLine("Modes:");
            Console.WriteLine("  http        - Run HTTP client demonstration");
            Console.WriteLine("  pipe        - Run Named Pipe client demonstration (Windows only)");
            Console.WriteLine("  test        - Run comprehensive test suite");
            Console.WriteLine("  interactive - Interactive mode for manual testing");
            Console.WriteLine("  benchmark   - Run performance benchmarks");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  CacheClientApp.Net48.exe http");
            Console.WriteLine("  CacheClientApp.Net48.exe pipe");
            Console.WriteLine("  CacheClientApp.Net48.exe test");
            Console.WriteLine("  CacheClientApp.Net48.exe interactive");
        }

        static async Task RunHttpClientDemo()
        {
            Console.WriteLine("=== HTTP Client Demonstration ===");
            Console.WriteLine();

            using (var client = new HttpCacheClient())
            {
                try
                {
                    // Health check
                    Console.WriteLine("Checking service health...");
                    var health = await client.GetHealthAsync();
                    Console.WriteLine($"Service Status: {health?.Status} ({health?.Mode})");
                    Console.WriteLine();

                    // Basic operations
                    Console.WriteLine("Performing basic cache operations...");
                    
                    // Set a complex object
                    var userData = new
                    {
                        UserId = 12345,
                        UserName = "john.doe",
                        Email = "john.doe@example.com",
                        Roles = new[] { "User", "Admin" },
                        LastLogin = DateTime.Now,
                        Preferences = new
                        {
                            Theme = "Dark",
                            Language = "en-US",
                            Notifications = true
                        }
                    };

                    await client.SetAsync("user:12345", userData, 3600);
                    Console.WriteLine("✅ Set user data with 1-hour TTL");

                    // Get the data back
                    var retrievedUser = await client.GetAsync("user:12345");
                    if (retrievedUser != null)
                    {
                        Console.WriteLine($"✅ Retrieved user data: {retrievedUser.Value}");
                        Console.WriteLine($"   TTL remaining: {retrievedUser.RemainingTtlSeconds} seconds");
                    }

                    // Set some products for search demo
                    await client.SetAsync("product:laptop", new { Name = "Gaming Laptop", Price = 1299.99, Category = "Electronics" });
                    await client.SetAsync("product:mouse", new { Name = "Wireless Mouse", Price = 29.99, Category = "Electronics" });
                    await client.SetAsync("product:book", new { Name = "C# Programming", Price = 49.99, Category = "Books" });

                    // Search for products
                    var products = await client.SearchAsync("product:.*");
                    Console.WriteLine($"✅ Found {products?.Count ?? 0} products:");
                    if (products != null)
                    {
                        foreach (var product in products)
                        {
                            Console.WriteLine($"   {product.Key}: {product.Value.Value}");
                        }
                    }

                    // Get statistics
                    var stats = await client.GetStatsAsync();
                    if (stats != null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("📊 Cache Statistics:");
                        Console.WriteLine($"   Total Keys: {stats.TotalKeys}");
                        Console.WriteLine($"   Hit Rate: {stats.HitRate:P2}");
                        Console.WriteLine($"   Memory Usage: {stats.MemoryUsageBytes / 1024.0 / 1024.0:F2} MB");
                        Console.WriteLine($"   Uptime: {TimeSpan.FromSeconds(stats.UptimeSeconds)}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("✅ HTTP Client demonstration completed successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ HTTP Client demonstration failed: {ex.Message}");
                }
            }
        }

        static async Task RunNamedPipeClientDemo()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Console.WriteLine("⚠️  Named Pipes are only supported on Windows.");
                return;
            }

            Console.WriteLine("=== Named Pipe Client Demonstration ===");
            Console.WriteLine();

            using (var client = new NamedPipeCacheClient())
            {
                try
                {
                    // Basic operations
                    Console.WriteLine("Performing Named Pipe cache operations...");
                    
                    var sessionData = new
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        UserId = 67890,
                        LoginTime = DateTime.Now,
                        IPAddress = "192.168.1.100",
                        UserAgent = "CacheClientApp.Net48/1.0"
                    };

                    await client.SetAsync("session:active", sessionData, 1800); // 30 minutes
                    Console.WriteLine("✅ Set session data via Named Pipe");

                    var retrievedSession = await client.GetAsync("session:active");
                    if (retrievedSession != null)
                    {
                        Console.WriteLine($"✅ Retrieved session data: {retrievedSession.Value}");
                    }

                    // Get statistics
                    var stats = await client.GetStatsAsync();
                    if (stats != null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("📊 Cache Statistics (via Named Pipe):");
                        Console.WriteLine($"   Total Keys: {stats.TotalKeys}");
                        Console.WriteLine($"   Hit Rate: {stats.HitRate:P2}");
                        Console.WriteLine($"   Memory Usage: {stats.MemoryUsageBytes / 1024.0 / 1024.0:F2} MB");
                    }

                    Console.WriteLine();
                    Console.WriteLine("✅ Named Pipe Client demonstration completed successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Named Pipe Client demonstration failed: {ex.Message}");
                    if (ex.Message.Contains("No process is on the other end of the pipe"))
                    {
                        Console.WriteLine("💡 Make sure the cache service is running in Named Pipe mode:");
                        Console.WriteLine("   dotnet run -- --pipe-mode");
                    }
                }
            }
        }

        static async Task RunAllTests()
        {
            Console.WriteLine("=== Running Comprehensive Test Suite ===");
            Console.WriteLine();

            using (var tests = new CacheClientTests())
            {
                await tests.RunHttpClientTestsAsync();
                Console.WriteLine();
                await tests.RunNamedPipeClientTestsAsync();
            }
        }

        static async Task RunInteractiveMode()
        {
            Console.WriteLine("=== Interactive Mode ===");
            Console.WriteLine("Commands: get <key>, set <key> <value>, remove <key>, exists <key>, stats, all, search <pattern>, flush, exit");
            Console.WriteLine();

            using (var client = new HttpCacheClient())
            {
                while (true)
                {
                    Console.Write("> ");
                    var input = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    var parts = input.Split(' ');
                    var command = parts[0].ToLowerInvariant();

                    try
                    {
                        switch (command)
                        {
                            case "exit":
                            case "quit":
                                return;

                            case "get":
                                if (parts.Length < 2)
                                {
                                    Console.WriteLine("Usage: get <key>");
                                    break;
                                }
                                var item = await client.GetAsync(parts[1]);
                                if (item != null)
                                {
                                    Console.WriteLine($"Key: {item.Key}");
                                    Console.WriteLine($"Value: {item.Value}");
                                    Console.WriteLine($"TTL: {item.RemainingTtlSeconds}s");
                                }
                                else
                                {
                                    Console.WriteLine("Key not found");
                                }
                                break;

                            case "set":
                                if (parts.Length < 3)
                                {
                                    Console.WriteLine("Usage: set <key> <value> [ttl]");
                                    break;
                                }
                                var ttl = parts.Length > 3 && int.TryParse(parts[3], out var t) ? t : (int?)null;
                                var success = await client.SetAsync(parts[1], parts[2], ttl);
                                Console.WriteLine(success ? "✅ Set successfully" : "❌ Set failed");
                                break;

                            case "remove":
                                if (parts.Length < 2)
                                {
                                    Console.WriteLine("Usage: remove <key>");
                                    break;
                                }
                                var removed = await client.RemoveAsync(parts[1]);
                                Console.WriteLine(removed ? "✅ Removed successfully" : "❌ Key not found");
                                break;

                            case "exists":
                                if (parts.Length < 2)
                                {
                                    Console.WriteLine("Usage: exists <key>");
                                    break;
                                }
                                var exists = await client.ExistsAsync(parts[1]);
                                Console.WriteLine($"Key exists: {exists}");
                                break;

                            case "stats":
                                var stats = await client.GetStatsAsync();
                                if (stats != null)
                                {
                                    Console.WriteLine($"Total Keys: {stats.TotalKeys}");
                                    Console.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}");
                                    Console.WriteLine($"Hit Rate: {stats.HitRate:P2}");
                                    Console.WriteLine($"Memory: {stats.MemoryUsageBytes / 1024.0 / 1024.0:F2} MB");
                                }
                                break;

                            case "all":
                                var allItems = await client.GetAllAsync();
                                if (allItems != null)
                                {
                                    Console.WriteLine($"Total items: {allItems.Count}");
                                    foreach (var kvp in allItems)
                                    {
                                        Console.WriteLine($"  {kvp.Key}: {kvp.Value.Value}");
                                    }
                                }
                                break;

                            case "search":
                                if (parts.Length < 2)
                                {
                                    Console.WriteLine("Usage: search <pattern>");
                                    break;
                                }
                                var searchResults = await client.SearchAsync(parts[1]);
                                if (searchResults != null)
                                {
                                    Console.WriteLine($"Found {searchResults.Count} matches:");
                                    foreach (var kvp in searchResults)
                                    {
                                        Console.WriteLine($"  {kvp.Key}: {kvp.Value.Value}");
                                    }
                                }
                                break;

                            case "flush":
                                var flushed = await client.FlushAsync();
                                Console.WriteLine(flushed ? "✅ Cache flushed" : "❌ Flush failed");
                                break;

                            default:
                                Console.WriteLine("Unknown command. Available: get, set, remove, exists, stats, all, search, flush, exit");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error: {ex.Message}");
                    }

                    Console.WriteLine();
                }
            }
        }

        static async Task RunBenchmark()
        {
            Console.WriteLine("=== Performance Benchmark (.NET Framework 4.8) ===");
            Console.WriteLine();

            const int operationCount = 500;

            using (var httpClient = new HttpCacheClient())
            {
                Console.WriteLine($"HTTP Client Benchmark ({operationCount} operations):");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var tasks = new Task[operationCount];

                // SET operations
                for (int i = 0; i < operationCount; i++)
                {
                    var key = $"benchmark:net48:{i}";
                    var value = new { Id = i, Data = $"Benchmark data {i}", Timestamp = DateTime.Now };
                    tasks[i] = httpClient.SetAsync(key, value);
                }

                await Task.WhenAll(tasks);
                var setTime = stopwatch.ElapsedMilliseconds;

                // GET operations
                stopwatch.Restart();
                for (int i = 0; i < operationCount; i++)
                {
                    var key = $"benchmark:net48:{i}";
                    tasks[i] = httpClient.GetAsync(key);
                }

                await Task.WhenAll(tasks);
                var getTime = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"  SET: {operationCount} operations in {setTime}ms ({operationCount * 1000.0 / setTime:F0} ops/sec)");
                Console.WriteLine($"  GET: {operationCount} operations in {getTime}ms ({operationCount * 1000.0 / getTime:F0} ops/sec)");
                Console.WriteLine($"  Total: {operationCount * 2} operations in {setTime + getTime}ms");
            }

            // Named Pipe benchmark (Windows only)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WriteLine();
                using (var pipeClient = new NamedPipeCacheClient())
                {
                    try
                    {
                        Console.WriteLine($"Named Pipe Client Benchmark ({operationCount / 2} operations):");

                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        var tasks = new Task[operationCount / 2];

                        for (int i = 0; i < operationCount / 2; i++)
                        {
                            var key = $"benchmark:pipe:net48:{i}";
                            var value = new { Id = i, Data = $"Pipe benchmark {i}" };
                            tasks[i] = pipeClient.SetAsync(key, value);
                        }

                        await Task.WhenAll(tasks);
                        stopwatch.Stop();

                        Console.WriteLine($"  Named Pipe: {operationCount / 2} operations in {stopwatch.ElapsedMilliseconds}ms ({(operationCount / 2) * 1000.0 / stopwatch.ElapsedMilliseconds:F0} ops/sec)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Named Pipe benchmark failed: {ex.Message}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("✅ Benchmark completed!");
        }
    }
}
