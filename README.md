# Self-hosted Distributed Cache Service

A lightweight, **performance-first**, high-throughput, self-hosted cache service built with .NET 8. Designed for key-value pair caching with minimal latency, efficient memory usage, and TTL (time-to-live) support.

## 🚀 Features

- **Dual Communication Protocols**:
  - **Named Pipes IPC** (default, Windows-only) - Ultra-fast local communication
  - **HTTP REST API** (optional fallback) - Cross-platform compatibility

- **High Performance**:
  - Optimized in-memory data structures (`ConcurrentDictionary`)
  - Lock-free reads where possible
  - Efficient background cleanup with minimal GC pressure
  - Fast JSON serialization via `System.Text.Json`

- **Core Functionality**:
  - Add/Get/Remove key-value pairs
  - TTL (Time-To-Live) expiration
  - Key existence checks
  - Flush all cache
  - Regex-based search
  - Real-time statistics (hits, misses, memory usage)
  - Background expiry cleanup

- **Security & Deployment**:
  - API Key authentication
  - Self-hosted Kestrel server
  - IIS deployment ready
  - Docker support
  - Comprehensive logging

## 📋 Requirements

- .NET 8.0 SDK or later
- Windows (for Named Pipes) or any OS supporting .NET 8 (for HTTP mode)
- Visual Studio 2022 or VS Code (optional)

## 🛠️ Quick Start

### 1. Clone and Build

```bash
git clone https://github.com/mujassir/distributed-cache.git
cd distributed-cache
dotnet restore
dotnet build
```

### 2. Run in Named Pipe Mode (Default - Windows Only)

```bash
dotnet run -- --pipe-mode
```

### 3. Run in HTTP Mode (Cross-platform)

```bash
dotnet run -- --http-mode
```

### 4. Run in Development Mode (Both protocols enabled)

```bash
dotnet run
```

The service will start and display:
```
=== Self-hosted Cache Service Starting ===
Mode: Named Pipe (Primary)
Environment: Development
Named Pipe: cachepipe
HTTP API: http://localhost:5000
TTL Cleanup Interval: 30s
API Keys Configured: 2
=== Cache Service Ready ===
```

## 🔧 Configuration

Edit `appsettings.json` to customize the service:

```json
{
  "CacheSettings": {
    "ExpirationScanIntervalSeconds": 60,
    "DefaultTtlSeconds": 3600,
    "MaxCacheSize": 10000,
    "NamedPipeName": "cachepipe",
    "EnableNamedPipes": true,
    "EnableHttpApi": false
  },
  "AllowedApiKeys": [
    "your-api-key-here",
    "dev-key-12345"
  ],
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

## 📡 API Reference

### HTTP REST API Endpoints

| Endpoint | Method | Description | Example |
|----------|--------|-------------|---------|
| `/cache` | POST | Add/update cache item | `{"key": "user:123", "value": "John", "ttl": 3600}` |
| `/cache/{key}` | GET | Get cache item | `/cache/user:123` |
| `/cache/{key}` | DELETE | Remove cache item | `/cache/user:123` |
| `/cache/exists/{key}` | GET | Check if key exists | `/cache/exists/user:123` |
| `/cache/flush` | POST | Clear all cache | - |
| `/cache/stats` | GET | Get cache statistics | - |
| `/cache/all` | GET | Get all cache items | - |
| `/cache/search?pattern=...` | GET | Search by regex pattern | `/cache/search?pattern=user:.*` |
| `/health` | GET | Health check | - |

### Authentication

All API requests require the `X-API-KEY` header:

```bash
curl -H "X-API-KEY: dev-key-12345" http://localhost:5000/cache/stats
```

## 💻 Client Usage

### C# Named Pipe Client (Primary - Windows)

```csharp
using SelfHostedCacheService.Clients;

var client = new NamedPipeCacheClient("cachepipe");

// Set a value with TTL
await client.SetAsync("user:123", new { Name = "John", Email = "john@example.com" }, 3600);

// Get a value
var user = await client.GetAsync("user:123");
if (user != null)
{
    Console.WriteLine($"Found: {user.Value}");
}

// Check existence
var exists = await client.ExistsAsync("user:123");

// Search with regex
var users = await client.SearchAsync("user:.*");

// Get statistics
var stats = await client.GetStatsAsync();
Console.WriteLine($"Hit Rate: {stats.HitRate:P2}");
```

### C# HTTP Client (Fallback)

```csharp
using SelfHostedCacheService.Clients;

var client = new HttpCacheClient("http://localhost:5000", "dev-key-12345");

// Same API as Named Pipe client
await client.SetAsync("product:456", "Laptop", 7200);
var product = await client.GetAsync("product:456");
```

### .NET Framework 4.8 Client Application

A complete client application is provided for .NET Framework 4.8 integration:

```bash
# Build and run the .NET 4.8 client (Windows)
cd CacheClientApp.Net48
build-and-run.bat

# Or use PowerShell
.\build-and-run.ps1
```

**Features:**
- ✅ **Dual Protocol Support**: HTTP REST API + Named Pipes IPC
- ✅ **Modern Async Patterns**: Full async/await in .NET Framework 4.8
- ✅ **Interactive Mode**: Command-line interface for testing
- ✅ **Performance Benchmarks**: Measure throughput and latency
- ✅ **Integration Examples**: ASP.NET Web Forms, WinForms, Windows Services

**Usage Examples:**

```csharp
// HTTP Client (.NET Framework 4.8)
using (var client = new HttpCacheClient())
{
    var userData = new { UserId = 123, Name = "John", Email = "john@example.com" };
    await client.SetAsync("user:123", userData, 3600);

    var user = await client.GetAsync("user:123");
    var stats = await client.GetStatsAsync();
}

// Named Pipe Client (Windows only)
using (var client = new NamedPipeCacheClient("cachepipe"))
{
    await client.SetAsync("session:abc", sessionData, 1800);
    var session = await client.GetAsync("session:abc");
}
```

**Performance (.NET Framework 4.8):**
- HTTP: ~1,500-2,000 ops/sec
- Named Pipes: ~8,000-12,000 ops/sec (Windows)

### Raw HTTP with curl

```bash
# Add item
curl -X POST http://localhost:5000/cache \
  -H "X-API-KEY: dev-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"key": "test", "value": "Hello World", "ttl": 300}'

# Get item
curl -H "X-API-KEY: dev-key-12345" http://localhost:5000/cache/test

# Get stats
curl -H "X-API-KEY: dev-key-12345" http://localhost:5000/cache/stats
```

## 🚀 Deployment

### IIS Deployment

1. **Publish the application**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Create IIS Application**:
   - Create a new Application Pool (set to "No Managed Code")
   - Create a new Website/Application pointing to the publish folder
   - Ensure the ASP.NET Core Hosting Bundle is installed

3. **Configure web.config** (automatically generated):
   ```xml
   <aspNetCore processPath="dotnet"
               arguments=".\SelfHostedCacheService.dll --http-mode"
               stdoutLogEnabled="false"
               hostingModel="inprocess">
   ```

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SelfHostedCacheService.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SelfHostedCacheService.dll", "--http-mode"]
```

### Windows Service

```bash
# Install as Windows Service
dotnet publish -c Release -r win-x64 --self-contained
sc create "CacheService" binPath="C:\path\to\SelfHostedCacheService.exe --pipe-mode"
sc start "CacheService"
```

## 🧪 Testing

### Run Unit Tests

```bash
dotnet test
```

### Load Testing with k6

```javascript
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 100 },
    { duration: '1m', target: 500 },
    { duration: '30s', target: 0 },
  ],
};

export default function() {
  const headers = { 'X-API-KEY': 'dev-key-12345' };

  // Set operation
  const setPayload = JSON.stringify({
    key: `test-${Math.random()}`,
    value: `value-${Math.random()}`,
    ttl: 300
  });

  const setResponse = http.post('http://localhost:5000/cache', setPayload, {
    headers: { ...headers, 'Content-Type': 'application/json' }
  });

  check(setResponse, {
    'set status is 200': (r) => r.status === 200,
  });

  // Get operation
  const getResponse = http.get('http://localhost:5000/cache/stats', { headers });
  check(getResponse, {
    'get status is 200': (r) => r.status === 200,
  });
}
```

### Postman Collection

Import `postman-collection.json` into Postman for interactive API testing.

## 📊 Performance Benchmarks

Typical performance on modern hardware:

| Operation | Named Pipes | HTTP API |
|-----------|-------------|----------|
| SET | ~50,000 ops/sec | ~25,000 ops/sec |
| GET | ~100,000 ops/sec | ~30,000 ops/sec |
| Memory Usage | ~50MB (10K items) | ~60MB (10K items) |
| Latency (P95) | <1ms | <5ms |

## 🔍 Monitoring

### Health Check

```bash
curl http://localhost:5000/health
```

Response:
```json
{
  "Status": "Healthy",
  "Timestamp": "2024-01-15T10:30:00Z",
  "Mode": "NamedPipe"
}
```

### Statistics Endpoint

```bash
curl -H "X-API-KEY: dev-key-12345" http://localhost:5000/cache/stats
```

Response:
```json
{
  "success": true,
  "data": {
    "total_keys": 1250,
    "hits": 8500,
    "misses": 1200,
    "hit_rate": 0.876,
    "uptime_seconds": 3600,
    "memory_usage_bytes": 52428800,
    "expired_keys_cleaned": 45,
    "last_cleanup": "2024-01-15T10:25:00Z",
    "server_start_time": "2024-01-15T09:30:00Z"
  }
}
```

## 🛡️ Security

- **API Key Authentication**: All HTTP endpoints require valid API key
- **Input Validation**: Comprehensive request validation
- **Rate Limiting**: Configure via reverse proxy (nginx, IIS)
- **HTTPS**: Enable in production environments
- **Network Security**: Named Pipes are local-only by design

## 🔧 Troubleshooting

### Common Issues

1. **Named Pipes not working**:
   - Ensure running on Windows
   - Check pipe name in configuration
   - Verify no other process is using the same pipe

2. **HTTP API returns 401**:
   - Check `X-API-KEY` header
   - Verify API key in `appsettings.json`

3. **High memory usage**:
   - Monitor via `/cache/stats` endpoint
   - Adjust TTL values
   - Consider implementing LRU eviction

4. **Performance issues**:
   - Use Named Pipes for local communication
   - Optimize TTL cleanup interval
   - Monitor GC pressure

### Logging

Enable detailed logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SelfHostedCacheService": "Debug"
    }
  }
}
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🏗️ .NET Framework 4.8 Client Application

A complete client application is included for integrating with legacy .NET Framework 4.8 applications:

### 📁 Client Structure
```
CacheClientApp.Net48/
├── Services/
│   ├── HttpCacheClient.cs          # HTTP REST client
│   └── NamedPipeCacheClient.cs     # Named Pipe IPC client
├── Models/CacheModels.cs           # Data models and DTOs
├── Tests/CacheClientTests.cs       # Comprehensive test suite
├── Program.cs                      # Demo application with multiple modes
├── App.config                      # Configuration settings
├── build-and-run.bat               # Windows build script
├── build-and-run.ps1               # PowerShell build script
└── README.md                       # Client documentation
```

### 🚀 Quick Start (.NET Framework 4.8)
```bash
# Windows - Build and run
cd CacheClientApp.Net48
build-and-run.bat

# Select from available modes:
# 1. HTTP Client Demo
# 2. Named Pipe Demo (Windows only)
# 3. Comprehensive Tests
# 4. Interactive Mode
# 5. Performance Benchmark
```

### 💻 Integration Examples

**ASP.NET Web Forms:**
```csharp
public partial class Default : System.Web.UI.Page
{
    private static readonly HttpCacheClient _cache = new HttpCacheClient();

    protected async void Page_Load(object sender, EventArgs e)
    {
        var sessionData = new { UserId = GetUserId(), LoginTime = DateTime.Now };
        await _cache.SetAsync($"session:{Session.SessionID}", sessionData, 1800);
    }
}
```

**WinForms Desktop:**
```csharp
public partial class MainForm : Form
{
    private readonly NamedPipeCacheClient _cache = new NamedPipeCacheClient();

    private async void SaveButton_Click(object sender, EventArgs e)
    {
        await _cache.SetAsync("user:preferences", GetUserPreferences());
        MessageBox.Show("Preferences cached!");
    }
}
```

**Performance:** HTTP (~1,500-2,000 ops/sec), Named Pipes (~8,000-12,000 ops/sec)

## 🔮 Roadmap

- [ ] Persistent storage backends (Redis, SQL Server)
- [ ] Cluster support with distributed consensus
- [ ] LRU/LFU eviction policies
- [ ] Web-based admin dashboard
- [ ] Prometheus metrics export
- [ ] JWT authentication
- [ ] Rate limiting middleware
- [ ] Compression support
- [ ] Encryption at rest

## 📞 Support

- Create an issue for bug reports
- Discussions for questions and ideas
- Wiki for additional documentation

---

**Built with ❤️ using .NET 8 and optimized for maximum performance**
