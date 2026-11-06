using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlashSale.Worker.Services;

/// <summary>
/// Background Service chay purge/archive bang Outbox dinh ky (moi 24 giay mot lan).
/// Chua xoa cac ghi Outbox da xu ly (Published/Failed) va cu hon Outbox:PurgeDays (mac dinh 30 ngay).
/// Dam bao khong xoa su kien chua xu ly (Pending) de tro NGU NGUON NFR-06 (khong mat don 202 Accepted).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: OutboxPurgeWorkerService, Outbox, Purge, AsyncWorker, PostgreSQLPrimary.
/// </summary>
public class OutboxPurgeWorkerService : BackgroundService
{
    private readonly ILogger<OutboxPurgeWorkerService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _purgeInterval;

    public OutboxPurgeWorkerService(
        ILogger<OutboxPurgeWorkerService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxPurgeOptions> options)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _purgeInterval = TimeSpan.FromHours(24); // chay purge moi 24 gio mot lan
    }

    /// <summary>
    /// Vong lap chinh cua Background Service: chay purge dinh ky cho den khi dung.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPurgeWorkerService is running. Purge interval: {Interval}.", _purgeInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi chay purge Outbox dinh ky.");
            }

            // Doi khoang cach tien tu den lan chay tiep theo (hoac khi yeu cau dung)
            await Task.Delay(_purgeInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPurgeWorkerService is stopping.");
    }

    /// <summary>
    /// Chay purge mot lan: tao scope de lay IOutboxPurgeService (scoped), goi PurgeProcessedOutboxesAsync.
    /// </summary>
    private async Task PurgeOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var purgeService = scope.ServiceProvider.GetRequiredService<IOutboxPurgeService>();
        await PurgeOnceAsync(_logger, purgeService, stoppingToken);
    }

    /// <summary>
    /// Chay purge mot lan voi IOutboxPurgeService da duoc cung cap (dung cho test, khong can IServiceScopeFactory).
    /// </summary>
    internal static async Task PurgeOnceAsync(
        ILogger<OutboxPurgeWorkerService> logger,
        IOutboxPurgeService purgeService,
        CancellationToken stoppingToken)
    {
        var deletedCount = await purgeService.PurgeProcessedOutboxesAsync(stoppingToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Da purge {DeletedCount} ghi Outbox da xu ly va cu han (PurgeDays).", deletedCount);
        }
        else
        {
            logger.LogDebug("Khong co ghi Outbox nao can purge.");
        }
    }
}
