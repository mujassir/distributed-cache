using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SelfHostedCacheService.Middleware;
using SelfHostedCacheService.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<CacheManager>();
builder.Services.AddHostedService<ExpirationWorker>();
builder.Services.AddControllers();

var app = builder.Build();

// Use API Key Middleware
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();