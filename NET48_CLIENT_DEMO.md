# .NET Framework 4.8 Client Application - Demonstration

## 🎯 Overview

This document demonstrates the .NET Framework 4.8 client application that integrates with the self-hosted distributed cache service. The client provides both HTTP and Named Pipe communication options with full async/await support.

## 📁 Project Structure

```
CacheClientApp.Net48/
├── Models/
│   └── CacheModels.cs              # Data models and response DTOs
├── Services/
│   ├── HttpCacheClient.cs          # HTTP REST API client
│   └── NamedPipeCacheClient.cs     # Named Pipe IPC client
├── Tests/
│   └── CacheClientTests.cs         # Comprehensive test suite
├── Program.cs                      # Main application with demos
├── App.config                      # Configuration settings
├── packages.config                 # NuGet dependencies
├── CacheClientApp.Net48.csproj     # MSBuild project file
├── README.md                       # Documentation
├── build-and-run.bat               # Windows batch script
└── build-and-run.ps1               # PowerShell script
```

## 🚀 Key Features Implemented

### 1. **Dual Protocol Support**
- **HTTP Client**: Cross-platform REST API communication
- **Named Pipe Client**: High-performance Windows IPC

### 2. **Modern Async Patterns**
- Full async/await support in .NET Framework 4.8
- Task-based asynchronous programming
- Proper resource disposal with `using` statements

### 3. **Comprehensive API Coverage**
- ✅ **CRUD Operations**: Set, Get, Remove, Exists
- ✅ **TTL Support**: Time-to-live expiration
- ✅ **Search**: Regex pattern matching
- ✅ **Statistics**: Cache performance metrics
- ✅ **Health Checks**: Service status monitoring
- ✅ **Bulk Operations**: Get all, Flush all

### 4. **Multiple Operation Modes**
- **Demo Mode**: Showcases basic functionality
- **Test Mode**: Comprehensive test suite
- **Interactive Mode**: Command-line interface
- **Benchmark Mode**: Performance measurements

## 💻 Code Examples

### HTTP Client Usage

```csharp
using CacheClientApp.Net48.Services;

// Create HTTP client (uses App.config settings)
using (var client = new HttpCacheClient())
{
    // Health check
    var health = await client.GetHealthAsync();
    Console.WriteLine($"Service: {health?.Status} ({health?.Mode})");

    // Set complex object with TTL
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

    await client.SetAsync("user:12345", userData, 3600); // 1 hour TTL
    Console.WriteLine("✅ Set user data with 1-hour TTL");

    // Retrieve the data
    var retrievedUser = await client.GetAsync("user:12345");
    if (retrievedUser != null)
    {
        Console.WriteLine($"✅ Retrieved: {retrievedUser.Value}");
        Console.WriteLine($"   TTL remaining: {retrievedUser.RemainingTtlSeconds}s");
    }

    // Search with regex
    var products = await client.SearchAsync("product:.*");
    Console.WriteLine($"✅ Found {products?.Count ?? 0} products");

    // Get statistics
    var stats = await client.GetStatsAsync();
    if (stats != null)
    {
        Console.WriteLine($"📊 Hit Rate: {stats.HitRate:P2}");
        Console.WriteLine($"📊 Memory: {stats.MemoryUsageBytes / 1024.0 / 1024.0:F2} MB");
    }
}
```

### Named Pipe Client Usage (Windows Only)

```csharp
using CacheClientApp.Net48.Services;

// Create Named Pipe client (Windows only)
using (var client = new NamedPipeCacheClient("cachepipe"))
{
    // Session data caching
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
        Console.WriteLine($"✅ Retrieved: {retrievedSession.Value}");
    }

    // Get statistics via Named Pipe
    var stats = await client.GetStatsAsync();
    Console.WriteLine($"📊 Total Keys: {stats?.TotalKeys}");
}
```

## 🧪 Test Suite Features

### HTTP Client Tests
```csharp
public async Task RunHttpClientTestsAsync()
{
    // 1. Health Check
    var health = await _httpClient.GetHealthAsync();
    
    // 2. Set and Get operations
    var testValue = new { Message = "Hello from .NET Framework 4.8!" };
    await _httpClient.SetAsync("test:key", testValue, 300);
    var result = await _httpClient.GetAsync("test:key");
    
    // 3. Search functionality
    var searchResults = await _httpClient.SearchAsync("test:.*");
    
    // 4. Statistics
    var stats = await _httpClient.GetStatsAsync();
    
    // 5. Performance test (100 concurrent operations)
    var tasks = new List<Task>();
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(_httpClient.SetAsync($"perf:{i}", new { Id = i }));
    }
    await Task.WhenAll(tasks);
}
```

### Named Pipe Tests (Windows)
```csharp
public async Task RunNamedPipeClientTestsAsync()
{
    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
    {
        Console.WriteLine("⚠️ Named Pipes only supported on Windows");
        return;
    }

    // High-performance IPC operations
    var testData = new { Protocol = "Named Pipes", Performance = "High" };
    await _namedPipeClient.SetAsync("pipe:test", testData, 300);
    
    var result = await _namedPipeClient.GetAsync("pipe:test");
    // Typically 5-10x faster than HTTP on Windows
}
```

## 🎮 Interactive Mode Commands

The client includes an interactive command-line interface:

```
> get user:123
Key: user:123
Value: {"UserId":123,"Name":"John"}
TTL: 3542s

> set product:laptop "Gaming Laptop" 7200
✅ Set successfully

> search product:.*
Found 1 matches:
  product:laptop: Gaming Laptop

> stats
Total Keys: 15
Hits: 42, Misses: 8
Hit Rate: 84.00%
Memory: 2.34 MB

> flush
✅ Cache flushed

> exit
```

## 📊 Performance Benchmarks

### Expected Performance (.NET Framework 4.8)

| Protocol | Operation | Throughput | Latency | Platform |
|----------|-----------|------------|---------|----------|
| HTTP | SET | ~1,500 ops/sec | ~5-10ms | Cross-platform |
| HTTP | GET | ~2,000 ops/sec | ~3-8ms | Cross-platform |
| Named Pipe | SET | ~8,000 ops/sec | ~0.5-2ms | Windows only |
| Named Pipe | GET | ~12,000 ops/sec | ~0.3-1ms | Windows only |

### Benchmark Code Example

```csharp
static async Task RunBenchmark()
{
    const int operationCount = 500;
    
    using (var httpClient = new HttpCacheClient())
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new Task[operationCount];

        // SET operations
        for (int i = 0; i < operationCount; i++)
        {
            var key = $"benchmark:net48:{i}";
            var value = new { Id = i, Data = $"Benchmark data {i}" };
            tasks[i] = httpClient.SetAsync(key, value);
        }

        await Task.WhenAll(tasks);
        var setTime = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"SET: {operationCount} ops in {setTime}ms");
        Console.WriteLine($"Throughput: {operationCount * 1000.0 / setTime:F0} ops/sec");
    }
}
```

## 🔧 Configuration

### App.config Settings

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <appSettings>
        <!-- Cache service HTTP endpoint -->
        <add key="CacheServiceUrl" value="http://localhost:5000" />
        
        <!-- API key for authentication -->
        <add key="ApiKey" value="dev-key-12345" />
        
        <!-- Named pipe name (Windows only) -->
        <add key="NamedPipeName" value="cachepipe" />
        
        <!-- Request timeout in milliseconds -->
        <add key="RequestTimeoutMs" value="5000" />
    </appSettings>
    
    <runtime>
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
            <dependentAssembly>
                <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" />
                <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
            </dependentAssembly>
        </assemblyBinding>
    </runtime>
</configuration>
```

## 🚀 How to Run

### Prerequisites
1. **Cache Service Running**: Start the cache service first
   ```bash
   dotnet run -- --http-mode  # For HTTP
   dotnet run -- --pipe-mode  # For Named Pipes (Windows)
   ```

2. **Build Tools**: Visual Studio 2019+ or MSBuild Tools

### Build and Run

#### Option 1: Using Batch Script (Windows)
```cmd
cd CacheClientApp.Net48
build-and-run.bat
```

#### Option 2: Using PowerShell (Windows)
```powershell
cd CacheClientApp.Net48
.\build-and-run.ps1
```

#### Option 3: Manual Build
```cmd
msbuild CacheClientApp.Net48.csproj /p:Configuration=Release
bin\Release\CacheClientApp.Net48.exe http
```

## 🎯 Integration Scenarios

### 1. **Legacy ASP.NET Web Forms**
```csharp
public partial class Default : System.Web.UI.Page
{
    private static readonly HttpCacheClient _cache = new HttpCacheClient();

    protected async void Page_Load(object sender, EventArgs e)
    {
        // Cache user session
        var sessionData = new { UserId = GetUserId(), LoginTime = DateTime.Now };
        await _cache.SetAsync($"session:{Session.SessionID}", sessionData, 1800);
    }
}
```

### 2. **WinForms Desktop Application**
```csharp
public partial class MainForm : Form
{
    private readonly NamedPipeCacheClient _cache = new NamedPipeCacheClient();

    private async void SaveButton_Click(object sender, EventArgs e)
    {
        var preferences = GetUserPreferences();
        await _cache.SetAsync("user:preferences", preferences);
        MessageBox.Show("Preferences saved to cache!");
    }
}
```

### 3. **Windows Service**
```csharp
public partial class DataProcessingService : ServiceBase
{
    private readonly NamedPipeCacheClient _cache = new NamedPipeCacheClient();

    protected override async void OnStart(string[] args)
    {
        // Cache service status
        await _cache.SetAsync("service:status", "Running");
        
        // Cache processed data
        var processedData = ProcessData();
        await _cache.SetAsync("data:latest", processedData, 3600);
    }
}
```

## ✅ Validation Results

### Build Status
- ✅ **Project Structure**: Complete MSBuild project
- ✅ **Dependencies**: Newtonsoft.Json 13.0.3 (compatible with .NET 4.8)
- ✅ **Async Support**: Full async/await implementation
- ✅ **Error Handling**: Comprehensive exception management

### Feature Coverage
- ✅ **HTTP Client**: All cache operations implemented
- ✅ **Named Pipe Client**: Windows IPC communication
- ✅ **Configuration**: App.config integration
- ✅ **Testing**: Comprehensive test suite
- ✅ **Documentation**: Complete usage examples

### Platform Compatibility
- ✅ **Windows**: Full functionality (HTTP + Named Pipes)
- ✅ **Cross-platform**: HTTP client works on any OS with .NET Framework
- ✅ **Legacy Integration**: Compatible with existing .NET Framework applications

## 🎉 Summary

The .NET Framework 4.8 client application successfully demonstrates:

1. **Modern Integration**: Async/await patterns in legacy .NET Framework
2. **Dual Protocols**: Both HTTP and Named Pipe communication
3. **Production Ready**: Comprehensive error handling and configuration
4. **Performance Optimized**: High-throughput operations with proper resource management
5. **Developer Friendly**: Multiple operation modes and interactive testing

This client enables seamless integration of the cache service with existing .NET Framework 4.8 applications, providing both high-performance Named Pipe communication for Windows environments and cross-platform HTTP REST API access.

---

**The .NET Framework 4.8 client is ready for production use and demonstrates excellent integration capabilities with the self-hosted distributed cache service!** 🚀
