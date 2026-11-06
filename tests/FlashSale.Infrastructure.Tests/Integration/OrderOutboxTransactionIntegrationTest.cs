using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

/// <summary>
/// Integration test kiem tra ghi Order + Outbox trong cung 1 transaction PostgreSQLPrimary
/// thuc su (Testcontainers), theo Transactional Outbox Pattern (FR-04, NFR-06).
/// </summary>
public class OrderOutboxTransactionIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private PostgreSQLPrimaryContext _context = null!;
    private IOrderRepository _orderRepository = null!;
    private IOutboxRepository _outboxRepository = null!;
    private IUnitOfWork _unitOfWork = null!;

    public OrderOutboxTransactionIntegrationTest()
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

        _orderRepository = new OrderRepository(_context);
        _outboxRepository = new OutboxRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    public async Task DisposeAsync()
    {
        _unitOfWork.Dispose();
        await _context.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task OrderAndOutbox_ShouldBeSavedInSameTransaction_WhenCommit()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var order = new Order(orderId, "product-1", 5, "client-1");
        order.UpdateStatus("Created");

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

        // Act: ghi Order + Outbox trong cung 1 transaction qua IUnitOfWork (Transactional Outbox Pattern)
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _orderRepository.AddAsync(order);
            await _outboxRepository.AddAsync(outbox);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        // Assert: Order duoc luu vao PostgreSQLPrimary
        var savedOrder = await _orderRepository.GetByIdAsync(orderId);
        savedOrder.Should().NotBeNull();
        savedOrder!.ProductId.Should().Be("product-1");
        savedOrder.Quantity.Should().Be(5);
        savedOrder.ClientId.Should().Be("client-1");
        savedOrder.OrderStatus.Should().Be("Created");

        // Assert: Outbox duoc luu trong cung transaction voi Order
        var savedOutbox = await _outboxRepository.GetByIdAsync(outbox.Id);
        savedOutbox.Should().NotBeNull();
        savedOutbox!.OrderId.Should().Be(orderId);
        savedOutbox.EventType.Should().Be("OrderCreated");
        savedOutbox.ProcessingStatus.Should().Be("Pending");
        savedOutbox.RetryCount.Should().Be(0);
        savedOutbox.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task Rollback_ShouldNotPersistOrderAndOutbox_WhenException()
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
        await _unitOfWork.BeginTransactionAsync();
        await _orderRepository.AddAsync(order);
        await _outboxRepository.AddAsync(outbox);
        await _unitOfWork.RollbackTransactionAsync();

        // Assert: khong co gi duoc luu (su dung AsNoTracking de truy van truc tiep DB, bo qua local cache)
        var savedOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        savedOrder.Should().BeNull();

        var savedOutbox = await _context.Outboxes.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == order.Id);
        savedOutbox.Should().BeNull();
    }
}
