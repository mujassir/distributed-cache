using SelfHostedCacheService.Middleware;
using SelfHostedCacheService.Services;

var builder = WebApplication.CreateBuilder(args);

// Parse command line arguments
var commandArgs = Environment.GetCommandLineArgs();
var pipeMode = commandArgs.Contains("--pipe-mode");
var httpMode = commandArgs.Contains("--http-mode") || !pipeMode; // Default to HTTP if no mode specified

// Configure services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Self-hosted Cache Service API", 
        Version = "v1",
        Description = "High-performance distributed key-value cache service with TTL support"
    });
    
    // Add API Key authentication to Swagger
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "API Key authentication"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// Register core services
builder.Services.AddSingleton<CacheManager>();
builder.Services.AddHostedService<ExpirationWorker>();

// Configure hosting mode
if (pipeMode || builder.Configuration.GetValue<bool>("CacheSettings:EnableNamedPipes", true))
{
    builder.Services.AddHostedService<NamedPipeServer>();
    Console.WriteLine("Named Pipe mode enabled");
}

if (httpMode || builder.Configuration.GetValue<bool>("CacheSettings:EnableHttpApi", false))
{
    Console.WriteLine("HTTP API mode enabled");
}
else if (pipeMode)
{
    // In pipe-only mode, disable HTTP endpoints
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.SuppressCheckForUnhandledSecurityMetadata = true;
    });
}

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure Kestrel for performance
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1MB
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("CacheSettings:EnableHttpApi", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cache Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// Add API key middleware for HTTP endpoints
if (httpMode || builder.Configuration.GetValue<bool>("CacheSettings:EnableHttpApi", false))
{
    app.UseMiddleware<ApiKeyMiddleware>();
}

app.UseRouting();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Mode = pipeMode ? "NamedPipe" : "HTTP"
});

// Display startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var configuration = app.Services.GetRequiredService<IConfiguration>();

logger.LogInformation("=== Self-hosted Cache Service Starting ===");
logger.LogInformation("Mode: {Mode}", pipeMode ? "Named Pipe (Primary)" : "HTTP API");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

if (pipeMode || configuration.GetValue<bool>("CacheSettings:EnableNamedPipes", true))
{
    var pipeName = configuration.GetValue<string>("CacheSettings:NamedPipeName", "cachepipe");
    logger.LogInformation("Named Pipe: {PipeName}", pipeName);
}

if (httpMode || configuration.GetValue<bool>("CacheSettings:EnableHttpApi", false))
{
    var urls = configuration.GetValue<string>("Kestrel:Endpoints:Http:Url", "http://localhost:5000");
    logger.LogInformation("HTTP API: {Urls}", urls);
}

var scanInterval = configuration.GetValue<int>("CacheSettings:ExpirationScanIntervalSeconds", 60);
logger.LogInformation("TTL Cleanup Interval: {Interval}s", scanInterval);

var apiKeys = configuration.GetSection("AllowedApiKeys").Get<string[]>() ?? Array.Empty<string>();
logger.LogInformation("API Keys Configured: {Count}", apiKeys.Length);

logger.LogInformation("=== Cache Service Ready ===");

// Handle graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Cache service is shutting down...");
});

lifetime.ApplicationStopped.Register(() =>
{
    logger.LogInformation("Cache service stopped");
});

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Cache service terminated unexpectedly");
    throw;
}

// Make Program class public for testing
public partial class Program { }
