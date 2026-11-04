using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.IO;

namespace FlashSale.Infrastructure;

/// <summary>
/// Extension methods de dang ky cac dich vu Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Đăng ký Redis connection
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";
        var redisConnection = ConnectionMultiplexer.ConnectAsync(redisConnectionString).GetAwaiter().GetResult();
        services.AddSingleton(redisConnection);

        // Đọc Lua script content
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var luaScriptPath = Path.Combine(repoRoot, "src", "FlashSale.Infrastructure", "ExternalServices", "Redis", "Scripts", "rate_limit_token_bucket.lua");
        var luaScriptContent = File.ReadAllText(luaScriptPath);

        // Đăng ký Lua script executor
        services.AddSingleton<IRedisScriptExecutor>(_ => new RedisScriptExecutor(redisConnection.GetDatabase()));

        // Đăng ký RateLimitRepository
        services.AddSingleton<IRateLimitRepository>(_ => new RedisRateLimitService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            luaScriptContent));

        return services;
    }
}
