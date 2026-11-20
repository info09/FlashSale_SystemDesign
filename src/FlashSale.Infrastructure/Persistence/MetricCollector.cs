using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IMetricCollector, thu thap metric tu cac dich vao infra (Redis, RabbitMQ)
/// de xuat ra Prometheus (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: MetricCollector, Redis, RabbitMQ, PostgreSQLPrimary.
/// Khong duoc goi la: MetricsCollector, MetricGatherer, StatisticsCollector.
/// </summary>
public class MetricCollector : IMetricCollector
{
    private readonly IDatabase _redisDatabase;
    private readonly IRabbitMqConnectionProvider _rabbitMqConnectionProvider;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<MetricCollector> _logger;

    public MetricCollector(
        IDatabase redisDatabase,
        IRabbitMqConnectionProvider rabbitMqConnectionProvider,
        IInventoryRepository inventoryRepository,
        ILogger<MetricCollector> logger)
    {
        _redisDatabase = redisDatabase;
        _rabbitMqConnectionProvider = rabbitMqConnectionProvider;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// Lay so luong ton kho kha dung con lai tren Redis (InventoryRemaining, available sau reserve hold).
    /// </summary>
    public async Task<double> GetInventoryRemainingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Lay tong so luong ton kho kha dung tu InventoryRepository (tung product, sau reserve/hold).
            // Neu khong co product nao, tra ve 0.
            var inventory = await _inventoryRepository.GetInventoryRemainingAsync(cancellationToken);
            return inventory;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay InventoryRemaining tu Redis.");
            return 0;
        }
    }

    /// <summary>
    /// Lay so luong message dang cho trong RabbitMQ Queue (order_pipeline_queue) (QueueDepth).
    /// Su dung QueueDeclare(passive: true) de lay thong tin queue khong tac dong den du lieu.
    /// Neu queue chua ton tai, tu dong khai bao moi de tranh loi.
    /// </summary>
    public Task<double> GetQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_rabbitMqConnectionProvider.TryGetConnection(out var connection) || connection is null || !connection.IsOpen)
                return Task.FromResult(0d);

            using var channel = connection.CreateModel();
            try
            {
                var result = channel.QueueDeclarePassive(RabbitMqConstants.QueueName);
                return Task.FromResult((double)result.MessageCount);
            }
            catch (Exception ex) when (ex.Message.Contains("NOT_FOUND") || ex.Message.Contains("404"))
            {
                // Queue chua ton tai, tu dong khai bao moi (trong rong)
                channel.QueueDeclare(
                    queue: RabbitMqConstants.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                return Task.FromResult(0d);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay QueueDepth tu RabbitMQ.");
            return Task.FromResult(0d);
        }
    }

    /// <summary>
    /// Lay so luong message trong Dead Letter Queue (order_pipeline_dlq) (DlqSize).
    /// Neu queue chua ton tai, tu dong khai bao moi de tranh loi.
    /// </summary>
    public Task<double> GetDlqSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_rabbitMqConnectionProvider.TryGetConnection(out var connection) || connection is null || !connection.IsOpen)
                return Task.FromResult(0d);

            using var channel = connection.CreateModel();
            try
            {
                var result = channel.QueueDeclarePassive(RabbitMqConstants.DeadLetterQueueName);
                return Task.FromResult((double)result.MessageCount);
            }
            catch (Exception ex) when (ex.Message.Contains("NOT_FOUND") || ex.Message.Contains("404"))
            {
                // Queue chua ton tai, tu dong khai bao moi (trong rong)
                channel.QueueDeclare(
                    queue: RabbitMqConstants.DeadLetterQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                return Task.FromResult(0d);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay DlqSize tu RabbitMQ.");
            return Task.FromResult(0d);
        }
    }
}
