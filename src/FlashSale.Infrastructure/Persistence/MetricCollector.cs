using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly IConnection _rabbitMqConnection;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<MetricCollector> _logger;

    public MetricCollector(
        IDatabase redisDatabase,
        IConnection rabbitMqConnection,
        IInventoryRepository inventoryRepository,
        ILogger<MetricCollector> logger)
    {
        _redisDatabase = redisDatabase;
        _rabbitMqConnection = rabbitMqConnection;
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
    public async Task<double> GetQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = _rabbitMqConnection.CreateModel();
            try
            {
                var result = channel.QueueDeclarePassive(RabbitMqConstants.QueueName);
                return result.MessageCount;
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
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay QueueDepth tu RabbitMQ.");
            return 0;
        }
    }

    /// <summary>
    /// Lay so luong message trong Dead Letter Queue (order_pipeline_dlq) (DlqSize).
    /// Neu queue chua ton tai, tu dong khai bao moi de tranh loi.
    /// </summary>
    public async Task<double> GetDlqSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = _rabbitMqConnection.CreateModel();
            try
            {
                var result = channel.QueueDeclarePassive(RabbitMqConstants.DeadLetterQueueName);
                return result.MessageCount;
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
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay DlqSize tu RabbitMQ.");
            return 0;
        }
    }
}
