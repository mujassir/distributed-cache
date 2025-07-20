# CacheClient (.NET 4.8)

A simple console app to test the SelfHostedCacheService HTTP API.

## Prerequisites
- .NET Framework 4.8
- Newtonsoft.Json NuGet package

## How to Build
1. Open the `CacheClient` folder in Visual Studio.
2. Add a reference to `Newtonsoft.Json` via NuGet.
3. Build the solution.

## How to Run
- Run the console app. It will:
  - Add a key to the cache
  - Retrieve the key
  - Search for the key
  - Delete the key

## Configuration
- The API key and base URL are set in `CacheClient.cs`.
- Ensure the cache service is running and the API key matches `appsettings.json` in the service.