using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

/// <summary>
/// Integration test kiem tra purge/archive bang Outbox (xoa ghi da xu ly cu hon PurgeDays)
/// voi PostgreSQLPrimary thuc su (Testcontainers), ho tro NFR-06.
/// </summary>
public class OutboxPurgeIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private PostgreSQLPrimaryContext _context = null!;
    private IOutboxRepository _outboxRepository = null!;

    public OutboxPurgeIntegrationTest()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("flashsale")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<PostgreSQLPrimaryContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new PostgreSQLPrimaryContext(options);
        await _context.Database.MigrateAsync();

        _outboxRepository = new OutboxRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task PurgeProcessedOutboxesOlderThanAsync_ShouldDeleteOnlyOldProcessedOutboxes()
    {
        // Arrange: PurgeDays = 30 -> xoa ghi Published/Failed co OccurredOn cu hon 30 ngay
        var olderThan = DateTimeOffset.UtcNow.AddDays(-31);

        // Ghi Published cu hon 30 ngay -> DUOC xoa
        var oldPublished = new Outbox(
            Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}",
            olderThan.AddDays(-1), Guid.NewGuid(), "Published", 0, DateTimeOffset.UtcNow.AddDays(-1));

        // Ghi Failed cu hon 30 ngay -> DUOC xoa
        var oldFailed = new Outbox(
            Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}",
            olderThan.AddDays(-2), Guid.NewGuid(), "Failed", 0, DateTimeOffset.UtcNow.AddDays(-2));

        // Ghi Published nhung moi hon 30 ngay -> KHONG duoc xoa
        var newPublished = new Outbox(
            Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}",
            DateTimeOffset.UtcNow.AddDays(-1), Guid.NewGuid(), "Published", 0, DateTimeOffset.UtcNow);

        // Ghi Pending (chua xu ly) cu hon 30 ngay -> KHONG duoc xoa (chua xu ly xong)
        var oldPending = new Outbox(
            Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}",
            olderThan.AddDays(-3), Guid.NewGuid(), "Pending", 0, null);

        await _context.Outboxes.AddRangeAsync(oldPublished, oldFailed, newPublished, oldPending);
        await _context.SaveChangesAsync();

        // Act
        var deletedCount = await _outboxRepository.PurgeProcessedOutboxesOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-30));

        // Assert: xoa dung 2 ghi Published/Failed cu
        deletedCount.Should().Be(2);

        // Assert: ghi moi va Pending con lai
        var remaining = await _context.Outboxes.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().Contain(o => o.Id == newPublished.Id && o.ProcessingStatus == "Published");
        remaining.Should().Contain(o => o.Id == oldPending.Id && o.ProcessingStatus == "Pending");
    }

    [Fact]
    public async Task PurgeProcessedOutboxesOlderThanAsync_WhenNoOldProcessedOutboxes_ShouldReturnZero()
    {
        // Arrange: chi co ghi Pending moi
        var newPending = new Outbox(
            Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}",
            DateTimeOffset.UtcNow, Guid.NewGuid(), "Pending", 0, null);
        await _context.Outboxes.AddAsync(newPending);
        await _context.SaveChangesAsync();

        // Act
        var deletedCount = await _outboxRepository.PurgeProcessedOutboxesOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-30));

        // Assert
        deletedCount.Should().Be(0);
        var remaining = await _context.Outboxes.CountAsync();
        remaining.Should().Be(1);
    }
}
