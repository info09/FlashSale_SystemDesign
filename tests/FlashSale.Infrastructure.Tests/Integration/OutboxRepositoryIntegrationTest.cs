using System.IO;
using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

public class OutboxRepositoryIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private PostgreSQLPrimaryContext _context = null!;

    public OutboxRepositoryIntegrationTest()
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
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task OrderAndOutbox_ShouldBeSavedInSameTransaction()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var order = new Order("product-1", 5, "client-1");
        order.UpdateStatus("Created");
        // Dat Id chinh xac de khoi tao tu DB
        order = new Order(orderId, "product-1", 5, "client-1");

        var outbox = new Outbox(
            Guid.NewGuid(),
            orderId,
            "OrderCreated",
            "{\"productId\":\"product-1\",\"quantity\":5,\"clientId\":\"client-1\",\"orderStatus\":\"Created\"}",
            DateTimeOffset.UtcNow,
            correlationId,
            "Pending",
            0,
            null);

        // Act: ghi Order + Outbox trong cung 1 transaction (Transactional Outbox Pattern)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Orders.Add(order);
            _context.Outboxes.Add(outbox);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Assert: Order duoc luu
        var savedOrder = await _context.Orders.FindAsync(orderId);
        savedOrder.Should().NotBeNull();
        savedOrder.ProductId.Should().Be("product-1");
        savedOrder.Quantity.Should().Be(5);
        savedOrder.ClientId.Should().Be("client-1");

        // Assert: Outbox duoc luu trong cung transaction
        var savedOutbox = await _context.Outboxes.FirstOrDefaultAsync(o => o.CorrelationId == correlationId);
        savedOutbox.Should().NotBeNull();
        savedOutbox.OrderId.Should().Be(orderId);
        savedOutbox.EventType.Should().Be("OrderCreated");
        savedOutbox.ProcessingStatus.Should().Be("Pending");
        savedOutbox.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task Rollback_ShouldNotPersistOrderAndOutbox()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "product-1", 5, "client-1");
        var outbox = new Outbox(
            Guid.NewGuid(),
            order.Id,
            "OrderCreated",
            "{\"productId\":\"product-1\"}",
            DateTimeOffset.UtcNow,
            order.Id,
            "Pending",
            0,
            null);

        // Act: bat dau transaction nhung rollback thay vi commit
        using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Orders.Add(order);
        _context.Outboxes.Add(outbox);
        await _context.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Assert: khong co gi duoc luu (dung AsNoTracking de truy van truc tiep DB, bo qua local cache)
        var savedOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        savedOrder.Should().BeNull();

        var savedOutbox = await _context.Outboxes.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == order.Id);
        savedOutbox.Should().BeNull();
    }
}
