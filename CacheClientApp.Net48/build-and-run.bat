@echo off
echo ========================================
echo Cache Service Client - .NET Framework 4.8
echo ========================================
echo.

REM Check if MSBuild is available
where msbuild >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: MSBuild not found in PATH
    echo Please install Visual Studio Build Tools or add MSBuild to PATH
    echo.
    echo You can download Visual Studio Build Tools from:
    echo https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022
    pause
    exit /b 1
)

echo Building the application...
msbuild CacheClientApp.Net48.csproj /p:Configuration=Release /p:Platform="Any CPU" /verbosity:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Build successful!
echo.

REM Check if cache service is running
echo Checking if cache service is running...
curl -s -H "X-API-KEY: dev-key-12345" http://localhost:5000/health >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo ✅ Cache service is running
) else (
    echo ⚠️  Cache service not detected at http://localhost:5000
    echo    Make sure to start the cache service first:
    echo    dotnet run -- --http-mode
    echo.
)

echo.
echo Available modes:
echo   1. HTTP Client Demo
echo   2. Named Pipe Demo (Windows only)
echo   3. Comprehensive Tests
echo   4. Interactive Mode
echo   5. Performance Benchmark
echo   6. Exit
echo.

:menu
set /p choice="Select mode (1-6): "

if "%choice%"=="1" (
    echo.
    echo Running HTTP Client Demo...
    echo.
    bin\Release\CacheClientApp.Net48.exe http
    goto :end
)

if "%choice%"=="2" (
    echo.
    echo Running Named Pipe Demo...
    echo.
    bin\Release\CacheClientApp.Net48.exe pipe
    goto :end
)

if "%choice%"=="3" (
    echo.
    echo Running Comprehensive Tests...
    echo.
    bin\Release\CacheClientApp.Net48.exe test
    goto :end
)

if "%choice%"=="4" (
    echo.
    echo Starting Interactive Mode...
    echo Type 'help' for available commands, 'exit' to quit
    echo.
    bin\Release\CacheClientApp.Net48.exe interactive
    goto :end
)

if "%choice%"=="5" (
    echo.
    echo Running Performance Benchmark...
    echo.
    bin\Release\CacheClientApp.Net48.exe benchmark
    goto :end
)

if "%choice%"=="6" (
    echo.
    echo Goodbye!
    goto :end
)

echo Invalid choice. Please select 1-6.
goto :menu

:end
echo.
echo Press any key to exit...
pause >nul
