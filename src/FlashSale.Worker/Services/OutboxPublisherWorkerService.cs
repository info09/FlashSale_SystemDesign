using FlashSale.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlashSale.Worker.Services;

/// <summary>
/// Background Service chay xuat phat (publish) cac ghi Outbox chua xu ly (Pending) ra RabbitMQ Queue dinh ky.
/// Dam bao 100% su kien da luu trong Outbox (don da nhan 202 Accepted) duoc xuat phat ra Queue (NFR-06, FR-04).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: OutboxPublisherWorkerService, Outbox, Publish, OrderPipeline, AsyncWorker, PostgreSQLPrimary.
/// </summary>
public class OutboxPublisherWorkerService : BackgroundService
{
    private readonly ILogger<OutboxPublisherWorkerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _publishInterval;

    public OutboxPublisherWorkerService(
        ILogger<OutboxPublisherWorkerService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _publishInterval = TimeSpan.FromSeconds(30); // chay publish moi 30 giay mot lan
    }

    /// <summary>
    /// Vong lap chinh cua Background Service: chay publish dinh ky cho den khi dung.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorkerService is running. Publish interval: {Interval}.", _publishInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi chay publish Outbox dinh ky.");
            }

            // Doi khoang cach tien tu den lan chay tiep theo (hoac khi yeu cau dung)
            await Task.Delay(_publishInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisherWorkerService is stopping.");
    }

    /// <summary>
    /// Chay publish mot lan: tao scope de lay IOutboxPublisher (scoped), goi PublishPendingOutboxAsync.
    /// </summary>
    private async Task PublishOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
        await PublishOnceAsync(_logger, publisher, stoppingToken);
    }

    /// <summary>
    /// Chay publish mot lan voi IOutboxPublisher da duoc cung cap (dung cho test, khong can IServiceScopeFactory).
    /// </summary>
    internal static async Task PublishOnceAsync(
        ILogger<OutboxPublisherWorkerService> logger,
        IOutboxPublisher publisher,
        CancellationToken stoppingToken)
    {
        await publisher.PublishPendingOutboxAsync(stoppingToken);
        logger.LogDebug("Da chay publish Outbox Pending mot lan.");
    }
}
