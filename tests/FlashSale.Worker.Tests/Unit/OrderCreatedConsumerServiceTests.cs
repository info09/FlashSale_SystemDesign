using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Worker.Consumers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class OrderCreatedConsumerServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly OrderCreatedConsumerService _consumer;

    public OrderCreatedConsumerServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _consumer = new OrderCreatedConsumerService(
            _orderRepositoryMock.Object,
            _outboxServiceMock.Object,
            _unitOfWorkMock.Object,
            new Mock<ILogger<OrderCreatedConsumerService>>().Object);
    }

    private static string BuildMessageJson(Guid orderId, string productId, int quantity, string clientId, string orderStatus = "Created")
    {
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;
        var message = new
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            OccurredOn = occurredOn,
            Payload = new
            {
                orderId,
                productId,
                quantity,
                clientId,
                orderStatus,
                createdOn = occurredOn
            }
        };
        return JsonSerializer.Serialize(message);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenOrderNotExists_ShouldCreateOrderAndOutboxInOneTransaction()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var json = BuildMessageJson(orderId, "product-1", 5, "client-1");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        await _consumer.ProcessMessageAsync(json);

        // Assert
        _orderRepositoryMock.Verify(x => x.AddAsync(It.Is<Order>(o =>
            o.Id == orderId &&
            o.ProductId == "product-1" &&
            o.Quantity == 5 &&
            o.ClientId == "client-1"), It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(x => x.AddOutboxAsync(
            orderId,
            "OrderProcessed",
            It.Is<string>(p => p.Contains("\"orderId\"")),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenOrderExists_ShouldSkipAndNotCommit()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var json = BuildMessageJson(orderId, "product-1", 5, "client-1");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order(orderId, "product-1", 5, "client-1"));

        // Act
        await _consumer.ProcessMessageAsync(json);

        // Assert
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxServiceMock.Verify(x => x.AddOutboxAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenCommitThrows_ShouldRollback()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var json = BuildMessageJson(orderId, "product-1", 5, "client-1");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _unitOfWorkMock
            .Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        var act = () => _consumer.ProcessMessageAsync(json);
        await act.Should().ThrowAsync<InvalidOperationException>();

        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
