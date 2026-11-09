using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Worker.Consumers;
using FlashSale.Worker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class OrderNotificationWorkerServiceTests
{
    private readonly Mock<IModel> _channelMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly OrderNotificationConsumerService _consumer;
    private readonly OrderNotificationWorkerService _worker;

    public OrderNotificationWorkerServiceTests()
    {
        _channelMock = new Mock<IModel>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _consumer = new OrderNotificationConsumerService(
            _orderRepositoryMock.Object,
            new Mock<ILogger<OrderNotificationConsumerService>>().Object);

        _worker = new OrderNotificationWorkerService(
            new Mock<ILogger<OrderNotificationWorkerService>>().Object,
            _channelMock.Object,
            _consumer);
    }

    private static byte[] BuildMessageBody(Guid orderId, string productId, int quantity, string clientId, Guid correlationId, Guid messageId)
    {
        var occurredOn = DateTimeOffset.UtcNow;
        var message = new
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            OccurredOn = occurredOn,
            Payload = new { orderId, productId, quantity, clientId, orderStatus = "Created", createdOn = occurredOn }
        };
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    }

    [Fact]
    public async Task HandleDeliveryAsync_WhenOrderExists_ShouldAckMessage()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var body = BuildMessageBody(orderId, "product-1", 5, "client-1", correlationId, messageId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order(orderId, "product-1", 5, "client-1"));

        var result = new BasicGetResult(1, false, "", "", 0, Mock.Of<IBasicProperties>(), body);

        // Act
        await _worker.HandleDeliveryAsync(result, CancellationToken.None);

        // Assert: Ack
        _channelMock.Verify(c => c.BasicAck(result.DeliveryTag, false), Times.Once);
        _channelMock.Verify(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandleDeliveryAsync_WhenConsumerThrows_ShouldNackMessage()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var body = BuildMessageBody(orderId, "product-1", 5, "client-1", correlationId, messageId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var result = new BasicGetResult(1, false, "", "", 0, Mock.Of<IBasicProperties>(), body);

        // Act
        await _worker.HandleDeliveryAsync(result, CancellationToken.None);

        // Assert: Nack (khong requeue)
        _channelMock.Verify(c => c.BasicNack(result.DeliveryTag, false, false), Times.Once);
        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
    }
}
