using FlashSale.Application.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FlashSale.Infrastructure.ExternalServices.RabbitMQ;

/// <summary>
/// Provider kết nối RabbitMQ lazy, thread-safe.
/// Kết nối chỉ được tạo khi có nhu cầu sử dụng (publish/health/metrics/worker).
/// </summary>
public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnectionProvider>? _logger;
    private readonly object _sync = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnectionProvider(string connectionString, ILogger<RabbitMqConnectionProvider>? logger = null)
        : this(new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync = false
        }, logger)
    {
    }

    public RabbitMqConnectionProvider(ConnectionFactory factory, ILogger<RabbitMqConnectionProvider>? logger = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        if (TryGetConnection(out var connection) && connection is not null)
            return connection;

        throw new InvalidOperationException("Không thể kết nối tới RabbitMQ.");
    }

    public bool TryGetConnection(out IConnection? connection)
    {
        if (_disposed)
        {
            connection = null;
            return false;
        }

        lock (_sync)
        {
            if (_disposed)
            {
                connection = null;
                return false;
            }

            if (_connection is { IsOpen: true })
            {
                connection = _connection;
                return true;
            }

            if (_connection is not null)
            {
                try { _connection.Dispose(); } catch { /* ignore */ }
                _connection = null;
            }

            try
            {
                _connection = _factory.CreateConnection();
                connection = _connection;
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Không thể tạo kết nối RabbitMQ.");
                connection = null;
                return false;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            try { _connection?.Close(); } catch { /* ignore */ }
            try { _connection?.Dispose(); } catch { /* ignore */ }
            _connection = null;
        }
    }
}
