using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlashSale.Infrastructure;

/// <summary>
/// Extension methods de dang ky cac dich vu Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitLuaScriptContent = FlashSale.Infrastructure.DependencyInjection.GetRateLimitLuaScriptContent();
        var inventoryLuaScriptContent = FlashSale.Infrastructure.DependencyInjection.GetInventoryLuaScriptContent();

        // Đăng ký Redis connection
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";
        var redisConnection = ConnectionMultiplexer.ConnectAsync(redisConnectionString).GetAwaiter().GetResult();
        services.AddSingleton(redisConnection);

        // Đăng ký Lua script executor
        services.AddSingleton<IRedisScriptExecutor>(_ => new RedisScriptExecutor(redisConnection.GetDatabase()));

        // Đăng ký RateLimitRepository với nội dung Lua script đã được đọc từ embedded resource
        services.AddSingleton<IRateLimitRepository>(_ => new RedisRateLimitService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            rateLimitLuaScriptContent));

        // Đăng ký InventoryRepository với nội dung Lua script Inventory đã được đọc từ embedded resource
        services.AddSingleton<IInventoryRepository>(_ => new RedisInventoryService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            inventoryLuaScriptContent));

        return services;
    }

    /// <summary>
    /// Đọc nội dung Lua script Rate Limit từ embedded resource của assembly Infrastructure.
    /// Hoạt động cả trong Docker container và trong test project.
    /// </summary>
    private static string GetRateLimitLuaScriptContent()
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var resourceName = "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.rate_limit_token_bucket.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Không tìm thấy embedded resource '{resourceName}' trong assembly '{assembly.FullName}'");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Đọc nội dung Lua script Inventory từ embedded resource của assembly Infrastructure.
    /// Hoạt động cả trong Docker container và trong test project.
    /// </summary>
    private static string GetInventoryLuaScriptContent()
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var resourceName = "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.atomic_inventory_deduct.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Không tìm thấy embedded resource '{resourceName}' trong assembly '{assembly.FullName}'");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
