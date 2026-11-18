using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Infrastructure.ExternalServices.Redis;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
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
        var holdCreateLuaScriptContent = GetHoldCreateLuaScriptContent();
        var holdConfirmLuaScriptContent = GetHoldConfirmLuaScriptContent();
        var holdReleaseLuaScriptContent = GetHoldReleaseLuaScriptContent();

        // Đăng ký Redis connection (AbortOnConnectFail = false de Worker co the khoi dong va retry ket noi neu Redis chua san sang)
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";
        var config = ConfigurationOptions.Parse(redisConnectionString);
        config.AbortOnConnectFail = false;
        var redisConnection = ConnectionMultiplexer.Connect(config);
        services.AddSingleton(redisConnection);

        // Đăng ký IDatabase (StackExchange.Redis IDatabase) cho HealthCheck
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());

        // Đăng ký Lua script executor
        services.AddSingleton<IRedisScriptExecutor>(_ => new RedisScriptExecutor(redisConnection.GetDatabase()));

        // Đăng ký RateLimitRepository với nội dung Lua script đã được đọc từ embedded resource
        services.AddSingleton<IRateLimitRepository>(_ => new RedisRateLimitService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            rateLimitLuaScriptContent));

        // Đăng ký InventoryRepository với nội dung Lua script Inventory đã được đọc từ embedded resource
        services.AddSingleton<IInventoryRepository>(_ => new RedisInventoryService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            inventoryLuaScriptContent,
            redisConnection.GetDatabase()));

        // Đăng ký HoldRepository (Redis atomic hold/reserve/confirm/release)
        services.AddSingleton<IHoldRepository>(_ => new RedisHoldService(
            new RedisScriptExecutor(redisConnection.GetDatabase()),
            holdCreateLuaScriptContent,
            holdConfirmLuaScriptContent,
            holdReleaseLuaScriptContent,
            redisConnection.GetDatabase()));

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

        // Đăng ký InformationCustomerRepository (luu thong tin khach hang khi confirm hold)
        services.AddScoped<IInformationCustomerRepository, InformationCustomerRepository>();

        // Đăng ký IDatabaseConnectionChecker (kiem tra ket noi PostgreSQLPrimary cho HealthCheck, NFR-07)
        services.AddScoped<IDatabaseConnectionChecker, PostgreSqlDatabaseConnectionChecker>();

        // Đăng ký HealthCheckService (kiem tra suc khoe cac dich vao, FR-06, NFR-07)
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // Đăng ký MetricCollector (thu thap metric tu infra, FR-06, NFR-07)
        services.AddScoped<IMetricCollector, MetricCollector>();

        // Đăng ký RabbitMqPublisherService (trien khai IRabbitMqPublisherService, publish OrderCreatedMessage vao Queue)
        var rabbitMqConnectionString = configuration["RABBITMQ_CONNECTION_STRING"] ?? "amqp://guest:guest@localhost:5672/";
        services.AddSingleton<IRabbitMqPublisherService>(_ => new RabbitMqPublisherService(rabbitMqConnectionString));

        // Đăng ký IConnection RabbitMQ (singleton) cho HealthCheck
        var rabbitFactory = new ConnectionFactory
        {
            Uri = new Uri(rabbitMqConnectionString),
            AutomaticRecoveryEnabled = false,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
        var rabbitConnection = rabbitFactory.CreateConnection();
        services.AddSingleton<RabbitMQ.Client.IConnection>(rabbitConnection);

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

    private static string GetHoldCreateLuaScriptContent()
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var resourceName = "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.hold_create.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Không tìm thấy embedded resource '{resourceName}' trong assembly '{assembly.FullName}'");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetHoldConfirmLuaScriptContent()
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var resourceName = "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.hold_confirm.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Không tìm thấy embedded resource '{resourceName}' trong assembly '{assembly.FullName}'");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetHoldReleaseLuaScriptContent()
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var resourceName = "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.hold_release.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Không tìm thấy embedded resource '{resourceName}' trong assembly '{assembly.FullName}'");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
