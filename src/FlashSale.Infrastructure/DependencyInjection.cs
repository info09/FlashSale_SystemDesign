using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlashSale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Đăng ký Redis connection
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton(redisConnection);
        // Đăng ký Lua script executor
        var luaScriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "rate_limit_token_bucket.lua");
        var luaScriptContent = File.ReadAllText(luaScriptPath);
        services.AddSingleton<IRedisScriptExecutor>(_ => new RedisScriptExecutor(redisConnection.GetDatabase()));
        services.AddSingleton<IRateLimitRepository>(_ => new RedisRateLimitService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            luaScriptContent));

        return services;
    }
}
