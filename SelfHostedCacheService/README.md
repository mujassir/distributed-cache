# SelfHostedCacheService

A high-performance, self-hosted distributed key-value cache service in .NET 8.

## Features
- In-memory cache with TTL (expiry)
- Thread-safe operations
- Hit/miss stats
- Regex-based search
- Background expiration
- Dual communication: Named Pipes (Windows) + HTTP (optional)
- API key authentication

## Setup
1. Ensure .NET 8 SDK is installed.
2. Configure your API key(s) in `appsettings.json` under `AllowedApiKeys`.
3. Run in HTTP mode:
   ```bash
   dotnet run
   ```
4. (Planned) Run in NamedPipe mode:
   ```bash
   dotnet run -- --pipe-mode
   ```

## HTTP API Endpoints
| Endpoint                    | Method | Description                  |
| --------------------------- | ------ | ---------------------------- |
| `/cache`                    | POST   | Add new key-value pair       |
| `/cache/{key}`              | GET    | Get value by key             |
| `/cache/{key}`              | DELETE | Remove key                   |
| `/cache/flush`              | POST   | Clear all keys               |
| `/cache/exists/{key}`       | GET    | Check if key exists          |
| `/cache/stats`              | GET    | Show cache statistics        |
| `/cache/all`                | GET    | Get all cache entries        |
| `/cache/search?pattern=...` | GET    | Get entries matching pattern |

## Authentication
- All requests require an `X-API-KEY` header with a valid key from `appsettings.json`.

## Future Work
- NamedPipeServer implementation
- Unit tests
- Example clients
- IIS deployment guide