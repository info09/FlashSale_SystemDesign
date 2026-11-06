using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Infrastructure.ExternalServices.Redis;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        var rateLimitLuaScriptContent = GetRateLimitLuaScriptContent();
        var inventoryLuaScriptContent = GetInventoryLuaScriptContent();

        // Đăng ký Redis connection (AbortOnConnectFail = false de Worker co the khoi dong va retry ket noi neu Redis chua san sang)
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";
        var config = ConfigurationOptions.Parse(redisConnectionString);
        config.AbortOnConnectFail = false;
        var redisConnection = ConnectionMultiplexer.Connect(config);
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

        // Đăng ký PostgreSQLPrimaryContext (ghi Order + Outbox + User qua Identity)
        var primaryConnectionString = configuration["POSTGRESQL_PRIMARY_CONNECTION_STRING"]
            ?? "Host=localhost;Port=5432;Database=flashsale;Username=postgres;Password=postgres";
        services.AddDbContext<PostgreSQLPrimaryContext>(options =>
            options.UseNpgsql(primaryConnectionString));

        // Đăng ký ASP.NET Core Identity voi ApplicationUser va IdentityRole<Guid>
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PostgreSQLPrimaryContext>()
            .AddDefaultTokenProviders();

        // Đăng ký UserRepository (trien khai IUserRepository)
        services.AddScoped<IUserRepository, UserRepository>();

        // Đăng ký AuthService (trien khai IUserService, su dung UserManager + SignInManager cua Identity)
        services.AddScoped<IUserService, AuthService>();

        // Đăng ký JwtTokenService (trien khai IJwtTokenService, ky JWT token cho Bearer authentication)
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Đăng ký OrderRepository (trien khai IOrderRepository)
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Đăng ký OutboxRepository (trien khai IOutboxRepository, Transactional Outbox Pattern)
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Đăng ký UnitOfWork (quan ly transaction PostgreSQLPrimary, Transactional Outbox Pattern)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Đăng ký ReconciliationRepository (doi chieu du lieu, NFR-06)
        services.AddScoped<IReconciliationRepository, ReconciliationRepository>();

        // Đăng ký RabbitMqPublisherService (trien khai IRabbitMqPublisherService, publish OrderCreatedMessage vao Queue)
        var rabbitMqConnectionString = configuration["RABBITMQ_CONNECTION_STRING"] ?? "amqp://guest:guest@localhost:5672/";
        services.AddSingleton<IRabbitMqPublisherService>(_ => new RabbitMqPublisherService(rabbitMqConnectionString));

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
