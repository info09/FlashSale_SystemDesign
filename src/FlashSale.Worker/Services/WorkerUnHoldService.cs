using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlashSale.Worker.Services;

/// <summary>
/// Background Service quét hold hết hạn và nhả slot/inventory (WorkerUnHold).
/// Quét định kỳ mỗi 1 giây dựa trên <c>holds:expiring</c> và gọi release idempotent.
/// </summary>
public class WorkerUnHoldService : BackgroundService
{
    private readonly ILogger<WorkerUnHoldService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _scanInterval;
    private readonly int _batchSize;

    /// <summary>
    /// Khởi tạo worker nhả hold hết hạn.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="serviceScopeFactory">Factory tạo scope để resolve dependencies.</param>
    public WorkerUnHoldService(
        ILogger<WorkerUnHoldService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _scanInterval = TimeSpan.FromSeconds(1);
        _batchSize = 100;
    }

    /// <summary>
    /// Vòng lặp quét và release hold hết hạn định kỳ.
    /// </summary>
    /// <param name="stoppingToken">Token dừng worker.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkerUnHoldService is running. Scan interval: {Interval}.", _scanInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredHoldsOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quét/release hold hết hạn.");
            }

            try
            {
                await Task.Delay(_scanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("WorkerUnHoldService is stopping.");
    }

    /// <summary>
    /// Thực hiện một lần quét các hold hết hạn và release idempotent.
    /// </summary>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    internal async Task ReleaseExpiredHoldsOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var holdRepository = scope.ServiceProvider.GetRequiredService<IHoldRepository>();
        await ReleaseExpiredHoldsOnceAsync(_logger, holdRepository, _batchSize, cancellationToken);
    }

    /// <summary>
    /// Quét và release các hold hết hạn một lần (dùng cho worker/test).
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="holdRepository">Repository thao tác hold.</param>
    /// <param name="batchSize">Số lượng hold tối đa xử lý mỗi lần.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    internal static async Task<int> ReleaseExpiredHoldsOnceAsync(
        ILogger logger,
        IHoldRepository holdRepository,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expiredHoldIds = await holdRepository.GetExpiredHoldIdsAsync(DateTimeOffset.UtcNow, batchSize, cancellationToken);
        var releasedCount = 0;

        foreach (var holdId in expiredHoldIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await holdRepository.ReleaseExpiredHoldAsync(holdId, cancellationToken);
            if (result.Status is Domain.Models.HoldReleaseStatus.Released
                or Domain.Models.HoldReleaseStatus.ExpiredReleased
                or Domain.Models.HoldReleaseStatus.AlreadyReleased
                or Domain.Models.HoldReleaseStatus.NotFound)
            {
                releasedCount++;
            }

            logger.LogDebug("Release expired hold {HoldId} => {Status}", holdId, result.Status);
        }

        if (expiredHoldIds.Count > 0)
        {
            logger.LogInformation("WorkerUnHold scanned {Count} expired hold(s), processed {Processed}.", expiredHoldIds.Count, releasedCount);
        }

        return releasedCount;
    }
}
