# SelfHostedCacheService: Windows Setup & Client Usage Guide

## 1. Prerequisites
- **Windows 10/11** (x64)
- **.NET 8 SDK** ([Download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- **Visual Studio 2022+** (for .NET 4.8 client, Community Edition is fine)

---

## 2. Installing .NET 8 SDK
1. Download the .NET 8 SDK installer from the [official site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
2. Run the installer and follow the prompts.
3. Verify installation:
   ```powershell
   dotnet --version
   ```
   You should see a version like `8.0.x`.

---

## 3. Configuring the Cache Service
1. Open the `SelfHostedCacheService/appsettings.json` file.
2. Set your desired API key(s) in the `AllowedApiKeys` array:
   ```json
   {
     "AllowedApiKeys": [
       "your-secret-api-key"
     ]
   }
   ```
3. Save the file.

---

## 4. Running the Cache Service
1. Open a terminal (Command Prompt or PowerShell).
2. Navigate to the `SelfHostedCacheService` directory:
   ```powershell
   cd path\to\SelfHostedCacheService
   ```
3. Run the service in HTTP mode:
   ```powershell
   dotnet run
   ```
   - The service will start on `http://localhost:5000` by default.
   - You should see logs indicating the service is running.

---

## 5. Testing the Service with the .NET 4.8 Client
### A. Setup
1. Open the `CacheClient` folder in Visual Studio.
2. Right-click the project > Manage NuGet Packages > Install `Newtonsoft.Json`.
3. In `CacheClient.cs`, ensure the API key matches the one in `appsettings.json`:
   ```csharp
   _httpClient.DefaultRequestHeaders.Add("X-API-KEY", "your-secret-api-key");
   ```
4. Build the solution.

### B. Run the Client
1. Start the cache service (if not already running).
2. Run the `CacheClient` console app (F5 or Ctrl+F5 in Visual Studio).
3. The app will:
   - Add a key-value pair
   - Retrieve the value
   - Search for the key
   - Delete the key
   - Print results to the console

---

## 6. Example HTTP Requests (with curl or Postman)
- **Add a key:**
  ```bash
  curl -X POST http://localhost:5000/cache -H "X-API-KEY: your-secret-api-key" -H "Content-Type: application/json" -d '{"key":"foo","value":"bar","ttl":60}'
  ```
- **Get a key:**
  ```bash
  curl http://localhost:5000/cache/foo -H "X-API-KEY: your-secret-api-key"
  ```
- **Delete a key:**
  ```bash
  curl -X DELETE http://localhost:5000/cache/foo -H "X-API-KEY: your-secret-api-key"
  ```

---

## 7. Troubleshooting
- **Port in use:** Change the port in `launchSettings.json` or via environment variable.
- **401 Unauthorized:** Ensure the API key in the client matches the service config.
- **.NET not found:** Ensure the .NET 8 SDK is installed and `dotnet` is in your PATH.

---

## 8. Next Steps
- For production, consider running as a Windows Service or behind IIS.
- See the main README for advanced features and future plans.