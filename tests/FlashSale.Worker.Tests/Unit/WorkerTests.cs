using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Worker.Consumers;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class WorkerTests
{
    private readonly Mock<IModel> _channelMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ExponentialBackoffPolicy _backoffPolicy;
    private readonly OrderCreatedConsumerService _consumer;
    private readonly Worker _worker;

    public WorkerTests()
    {
        _channelMock = new Mock<IModel>();
        _channelMock.Setup(c => c.CreateBasicProperties()).Returns(Mock.Of<IBasicProperties>());
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _backoffPolicy = new ExponentialBackoffPolicy(3, TimeSpan.FromMilliseconds(1));

        _consumer = new OrderCreatedConsumerService(
            _orderRepositoryMock.Object,
            _outboxServiceMock.Object,
            _unitOfWorkMock.Object,
            new Mock<ILogger<OrderCreatedConsumerService>>().Object);

        _worker = new Worker(
            Mock.Of<ILogger<Worker>>(),
            _channelMock.Object,
            _consumer,
            _backoffPolicy);
    }

    private static string BuildMessageJson(Guid orderId, string productId, int quantity, string clientId)
    {
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
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

    private static BasicGetResult BuildDelivery(byte[] body, int retryCount)
    {
        var headers = retryCount > 0
            ? new Dictionary<string, object> { { "x-retry-count", retryCount } }
            : null;

        var propsMock = new Mock<IBasicProperties>();
        propsMock.SetupGet(p => p.Headers).Returns(headers!);
        propsMock.SetupGet(p => p.Persistent).Returns(true);
        propsMock.SetupGet(p => p.ContentType).Returns("application/json");
        propsMock.SetupGet(p => p.MessageId).Returns(Guid.NewGuid().ToString());
        var properties = propsMock.Object;

        return new BasicGetResult(1, false, "", "", 0, properties, body);
    }

    [Fact]
    public async Task HandleDeliveryAsync_WhenRetryExhausted_ShouldPublishToDeadLetterQueueAndAck()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes(BuildMessageJson(orderId, "product-1", 1, "client-1"));
        var delivery = BuildDelivery(body, retryCount: 3); // da vuot qua gioi han 3 lan

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        await _worker.HandleDeliveryAsync(delivery, CancellationToken.None);

        // Assert: publish vao DeadLetterExchange (khong la exchange chinh)
        _channelMock.Verify(c => c.BasicPublish(
                It.Is<string>(ex => ex == RabbitMqConstants.DeadLetterExchangeName),
                It.Is<string>(rk => rk == RabbitMqConstants.DeadLetterRoutingKey),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.Is<ReadOnlyMemory<byte>>(b => b.Length == body.Length)),
            Times.Once);

        // Assert: Ack de xoa message khoi Queue chinh (da luu vao DLQ)
        _channelMock.Verify(c => c.BasicAck(delivery.DeliveryTag, false), Times.Once);

        // Assert: khong Nack
        _channelMock.Verify(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandleDeliveryAsync_WhenCanRetry_ShouldRepublishToMainExchangeAndNack()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes(BuildMessageJson(orderId, "product-1", 1, "client-1"));
        var delivery = BuildDelivery(body, retryCount: 0);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        await _worker.HandleDeliveryAsync(delivery, CancellationToken.None);

        // Assert: republish vao exchange chinh voi retry count tang len
        _channelMock.Verify(c => c.BasicPublish(
                It.Is<string>(ex => ex == RabbitMqConstants.ExchangeName),
                It.Is<string>(rk => rk == RabbitMqConstants.RoutingKey),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.Is<ReadOnlyMemory<byte>>(b => b.Length == body.Length)),
            Times.Once);

        // Assert: Nack (khong requeue)
        _channelMock.Verify(c => c.BasicNack(delivery.DeliveryTag, false, false), Times.Once);

        // Assert: khong Ack
        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandleDeliveryAsync_WhenProcessingSucceeds_ShouldAckAndNotPublishToDLQ()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes(BuildMessageJson(orderId, "product-1", 1, "client-1"));
        var delivery = BuildDelivery(body, retryCount: 0);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _worker.HandleDeliveryAsync(delivery, CancellationToken.None);

        // Assert: Ack
        _channelMock.Verify(c => c.BasicAck(delivery.DeliveryTag, false), Times.Once);

        // Assert: khong publish vao DLQ
        _channelMock.Verify(c => c.BasicPublish(
                It.Is<string>(ex => ex == RabbitMqConstants.DeadLetterExchangeName),
                It.Is<string>(rk => rk == RabbitMqConstants.DeadLetterRoutingKey),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()),
            Times.Never);
    }
}
