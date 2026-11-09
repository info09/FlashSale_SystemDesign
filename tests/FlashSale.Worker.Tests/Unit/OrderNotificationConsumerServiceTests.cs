using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Worker.Consumers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class OrderNotificationConsumerServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly OrderNotificationConsumerService _consumer;

    public OrderNotificationConsumerServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _consumer = new OrderNotificationConsumerService(
            _orderRepositoryMock.Object,
            new Mock<ILogger<OrderNotificationConsumerService>>().Object);
    }

    private static string BuildMessageJson(Guid orderId, string productId, int quantity, string clientId, Guid correlationId, Guid messageId)
    {
        var occurredOn = DateTimeOffset.UtcNow;
        var message = new
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            OccurredOn = occurredOn,
            Payload = new { orderId, productId, quantity, clientId, orderStatus = "Created", createdOn = occurredOn }
        };
        return JsonSerializer.Serialize(message);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenOrderExists_ShouldLogNotification()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var json = BuildMessageJson(orderId, "product-1", 5, "client-1", correlationId, messageId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order(orderId, "product-1", 5, "client-1"));

        // Act
        await _consumer.ProcessMessageAsync(json, CancellationToken.None);

        // Assert: GetByIdAsync duoc goi
        _orderRepositoryMock.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenOrderNotExists_ShouldSkipAndNotThrow()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var json = BuildMessageJson(orderId, "product-1", 5, "client-1", correlationId, messageId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act & Assert: khong ném exception
        await _consumer.ProcessMessageAsync(json, CancellationToken.None);

        _orderRepositoryMock.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
