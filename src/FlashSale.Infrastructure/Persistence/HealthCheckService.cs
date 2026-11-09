using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Dich vu HealthCheck o lop Infrastructure, kiem tra tinh trang suc khoe
/// cua cac dich vao: PostgreSQLPrimary, Redis, RabbitMQ (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: HealthCheckService, HealthCheck.
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly IDatabaseConnectionChecker _databaseConnectionChecker;
    private readonly IDatabase _redisDatabase;
    private readonly RabbitMQ.Client.IConnection _rabbitMqConnection;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        IDatabaseConnectionChecker databaseConnectionChecker,
        IDatabase redisDatabase,
        RabbitMQ.Client.IConnection rabbitMqConnection,
        ILogger<HealthCheckService> logger)
    {
        _databaseConnectionChecker = databaseConnectionChecker;
        _redisDatabase = redisDatabase;
        _rabbitMqConnection = rabbitMqConnection;
        _logger = logger;
    }

    /// <summary>
    /// Kiem tra tinh trang suc khoe cua tat ca cac dich vao.
    /// </summary>
    public async Task<HealthResult> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var checks = new Dictionary<string, object>();

        var postgresHealthy = await _databaseConnectionChecker.CanConnectAsync(cancellationToken);
        checks["PostgreSQLPrimary"] = postgresHealthy ? "Healthy" : "Unhealthy";

        var redisHealthy = await CheckRedisAsync(cancellationToken);
        checks["Redis"] = redisHealthy ? "Healthy" : "Unhealthy";

        var rabbitMqHealthy = CheckRabbitMQ();
        checks["RabbitMQ"] = rabbitMqHealthy ? "Healthy" : "Unhealthy";

        var allHealthy = postgresHealthy && redisHealthy && rabbitMqHealthy;

        return new HealthResult
        {
            Status = allHealthy ? "Healthy" : "Unhealthy",
            Checks = checks
        };
    }

    private async Task<bool> CheckRedisAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _redisDatabase.PingAsync();
            return result > TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check Redis that bai.");
            return false;
        }
    }

    private bool CheckRabbitMQ()
    {
        try
        {
            return _rabbitMqConnection.IsOpen;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check RabbitMQ that bai.");
            return false;
        }
    }
}
