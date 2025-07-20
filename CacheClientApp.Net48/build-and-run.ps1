# Cache Service Client - .NET Framework 4.8 Build and Run Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cache Service Client - .NET Framework 4.8" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if MSBuild is available
$msbuildPath = Get-Command msbuild -ErrorAction SilentlyContinue
if (-not $msbuildPath) {
    Write-Host "ERROR: MSBuild not found in PATH" -ForegroundColor Red
    Write-Host "Please install Visual Studio Build Tools or add MSBuild to PATH" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "You can download Visual Studio Build Tools from:" -ForegroundColor Yellow
    Write-Host "https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022" -ForegroundColor Blue
    Read-Host "Press Enter to exit"
    exit 1
}

# Build the application
Write-Host "Building the application..." -ForegroundColor Yellow
$buildResult = & msbuild CacheClientApp.Net48.csproj /p:Configuration=Release /p:Platform="Any CPU" /verbosity:minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""

# Check if cache service is running
Write-Host "Checking if cache service is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Headers @{"X-API-KEY"="dev-key-12345"} -TimeoutSec 3 -ErrorAction Stop
    Write-Host "✅ Cache service is running" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Cache service not detected at http://localhost:5000" -ForegroundColor Yellow
    Write-Host "   Make sure to start the cache service first:" -ForegroundColor Yellow
    Write-Host "   dotnet run -- --http-mode" -ForegroundColor Cyan
    Write-Host ""
}

# Display menu
Write-Host ""
Write-Host "Available modes:" -ForegroundColor Cyan
Write-Host "  1. HTTP Client Demo" -ForegroundColor White
Write-Host "  2. Named Pipe Demo (Windows only)" -ForegroundColor White
Write-Host "  3. Comprehensive Tests" -ForegroundColor White
Write-Host "  4. Interactive Mode" -ForegroundColor White
Write-Host "  5. Performance Benchmark" -ForegroundColor White
Write-Host "  6. Exit" -ForegroundColor White
Write-Host ""

do {
    $choice = Read-Host "Select mode (1-6)"
    
    switch ($choice) {
        "1" {
            Write-Host ""
            Write-Host "Running HTTP Client Demo..." -ForegroundColor Yellow
            Write-Host ""
            & "bin\Release\CacheClientApp.Net48.exe" http
            break
        }
        "2" {
            Write-Host ""
            Write-Host "Running Named Pipe Demo..." -ForegroundColor Yellow
            Write-Host ""
            & "bin\Release\CacheClientApp.Net48.exe" pipe
            break
        }
        "3" {
            Write-Host ""
            Write-Host "Running Comprehensive Tests..." -ForegroundColor Yellow
            Write-Host ""
            & "bin\Release\CacheClientApp.Net48.exe" test
            break
        }
        "4" {
            Write-Host ""
            Write-Host "Starting Interactive Mode..." -ForegroundColor Yellow
            Write-Host "Type 'help' for available commands, 'exit' to quit" -ForegroundColor Gray
            Write-Host ""
            & "bin\Release\CacheClientApp.Net48.exe" interactive
            break
        }
        "5" {
            Write-Host ""
            Write-Host "Running Performance Benchmark..." -ForegroundColor Yellow
            Write-Host ""
            & "bin\Release\CacheClientApp.Net48.exe" benchmark
            break
        }
        "6" {
            Write-Host ""
            Write-Host "Goodbye!" -ForegroundColor Green
            exit 0
        }
        default {
            Write-Host "Invalid choice. Please select 1-6." -ForegroundColor Red
            continue
        }
    }
    break
} while ($true)

Write-Host ""
Read-Host "Press Enter to exit"
