using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Worker.Consumers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class DeadLetterQueueIntegrationTest : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private IConnection _connection = null!;
    private IModel _channel = null!;

    public DeadLetterQueueIntegrationTest()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
            AutomaticRecoveryEnabled = false
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        DeclareInfrastructure();
    }

    private void DeclareInfrastructure()
    {
        // Exchange chinh
        _channel.ExchangeDeclare(
            exchange: RabbitMqConstants.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Dead Letter Exchange
        _channel.ExchangeDeclare(
            exchange: RabbitMqConstants.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Queue chinh voi DLX arguments
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchangeName },
            { "x-dead-letter-routing-key", RabbitMqConstants.DeadLetterRoutingKey },
            { "x-max-length", 100000 },
            { "x-message-ttl", 86400000 }
        };
        _channel.QueueDeclare(
            queue: RabbitMqConstants.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);
        _channel.QueueBind(
            queue: RabbitMqConstants.QueueName,
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey);

        // Dead Letter Queue
        _channel.QueueDeclare(
            queue: RabbitMqConstants.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.QueueBind(
            queue: RabbitMqConstants.DeadLetterQueueName,
            exchange: RabbitMqConstants.DeadLetterExchangeName,
            routingKey: RabbitMqConstants.DeadLetterRoutingKey);
    }

    public async Task DisposeAsync()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        await _rabbitMqContainer.StopAsync();
        await _rabbitMqContainer.DisposeAsync();
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

    private void PurgeQueues()
    {
        _channel.QueuePurge(queue: RabbitMqConstants.QueueName);
        _channel.QueuePurge(queue: RabbitMqConstants.DeadLetterQueueName);
    }

    [Fact]
    public async Task Message_After3FailedRetries_ShouldBeMovedToDeadLetterQueue()
    {
        // Arrange
        PurgeQueues();

        var orderId = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes(BuildMessageJson(orderId, "product-1", 1, "client-1"));

        // Consumer dependencies bi loi -> xu ly message that bai luon
        var orderRepositoryMock = new Mock<IOrderRepository>();
        var outboxServiceMock = new Mock<IOutboxService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var consumer = new OrderCreatedConsumerService(
            orderRepositoryMock.Object,
            outboxServiceMock.Object,
            unitOfWorkMock.Object,
            new Mock<ILogger<OrderCreatedConsumerService>>().Object);
        var backoff = new ExponentialBackoffPolicy(3, TimeSpan.FromMilliseconds(1));
        var worker = new FlashSale.Worker.Worker(
            Mock.Of<ILogger<FlashSale.Worker.Worker>>(),
            _channel,
            consumer,
            backoff);

        // Publish message vao Queue chinh voi retry count = 3 (da het lan thu lai)
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString();
        props.Headers = new Dictionary<string, object> { { "x-retry-count", 3 } };
        _channel.BasicPublish(
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        // Act: lay message tu Queue chinh va xu ly (retry exhausted -> chuyen vao DLQ)
        var result = _channel.BasicGet(queue: RabbitMqConstants.QueueName, autoAck: false);
        result.Should().NotBeNull("Queue chinh phai co message can xu ly");
        await worker.HandleDeliveryAsync(result!, CancellationToken.None);

        // Assert: message da duoc chuyen vao DeadLetterQueue
        var dlqResult = _channel.BasicGet(queue: RabbitMqConstants.DeadLetterQueueName, autoAck: false);
        dlqResult.Should().NotBeNull("DeadLetterQueue phai chua message sau 3 lan that bai");
        var dlqBody = Encoding.UTF8.GetString(dlqResult!.Body.ToArray());
        dlqBody.Should().Contain("product-1");
        dlqResult.BasicProperties.Headers.Should().ContainKey("x-retry-count");

        // Assert: Queue chinh da duoc Ack (khong con message)
        var queueResult = _channel.BasicGet(queue: RabbitMqConstants.QueueName, autoAck: false);
        queueResult.Should().BeNull("Queue chinh khong con message sau khi chuyen DLQ");
    }
}
