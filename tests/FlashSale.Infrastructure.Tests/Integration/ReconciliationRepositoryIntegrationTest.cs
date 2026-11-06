using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

/// <summary>
/// Integration test kiem tra ReconciliationRepository truy van thuc su tu PostgreSQLPrimary
/// (Testcontainers), ho tro quy trinh Reconciliation (NFR-06).
/// </summary>
public class ReconciliationRepositoryIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private PostgreSQLPrimaryContext _context = null!;
    private IReconciliationRepository _reconciliationRepository = null!;

    public ReconciliationRepositoryIntegrationTest()
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

        _reconciliationRepository = new ReconciliationRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetAllOrderIdsAsync_ShouldReturnAllOrderIdsInPostgreSQL()
    {
        // Arrange
        var order1 = new Order(Guid.NewGuid(), "product-1", 5, "client-1");
        var order2 = new Order(Guid.NewGuid(), "product-2", 3, "client-2");
        await _context.Orders.AddRangeAsync(order1, order2);
        await _context.SaveChangesAsync();

        // Act
        var orderIds = await _reconciliationRepository.GetAllOrderIdsAsync();

        // Assert
        orderIds.Should().HaveCount(2);
        orderIds.Should().Contain(order1.Id);
        orderIds.Should().Contain(order2.Id);
    }

    [Fact]
    public async Task GetAllOutboxesAsync_ShouldReturnAllOutboxesInPostgreSQL()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var outbox1 = new Outbox(
            Guid.NewGuid(), orderId, "OrderCreated", "{}", DateTimeOffset.UtcNow, correlationId, "Pending", 0, null);
        var outbox2 = new Outbox(
            Guid.NewGuid(), orderId, "OrderCreated", "{}", DateTimeOffset.UtcNow, correlationId, "Published", 0, DateTimeOffset.UtcNow);
        await _context.Outboxes.AddRangeAsync(outbox1, outbox2);
        await _context.SaveChangesAsync();

        // Act
        var outboxes = await _reconciliationRepository.GetAllOutboxesAsync();

        // Assert
        outboxes.Should().HaveCount(2);
        outboxes.Should().Contain(o => o.Id == outbox1.Id && o.ProcessingStatus == "Pending");
        outboxes.Should().Contain(o => o.Id == outbox2.Id && o.ProcessingStatus == "Published");
    }
}
