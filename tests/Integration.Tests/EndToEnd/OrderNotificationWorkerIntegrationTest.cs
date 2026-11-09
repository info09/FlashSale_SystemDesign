using FlashSale.Domain;
using FlashSale.Domain.Interfaces;
using FlashSale.Worker.Consumers;
using FlashSale.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class OrderNotificationWorkerIntegrationTest : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private IConnection _connection = null!;
    private IModel _channel = null!;

    public OrderNotificationWorkerIntegrationTest()
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

        // Khai bao Order Notification Exchange/Queue
        _channel.ExchangeDeclare(
            exchange: RabbitMqConstants.OrderNotificationExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);
        _channel.QueueDeclare(
            queue: RabbitMqConstants.OrderNotificationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.QueueBind(
            queue: RabbitMqConstants.OrderNotificationQueueName,
            exchange: RabbitMqConstants.OrderNotificationExchangeName,
            routingKey: RabbitMqConstants.OrderNotificationRoutingKey);
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
    public async Task Message_PublishedToOrderNotificationQueue_ShouldBeConsumedAndAcked()
    {
        // Arrange: publish message vao Queue order_notification_queue
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var body = BuildMessageBody(orderId, "product-1", 5, "client-1", correlationId, messageId);

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = messageId.ToString();
        props.CorrelationId = correlationId.ToString();

        _channel.BasicPublish(
            exchange: RabbitMqConstants.OrderNotificationExchangeName,
            routingKey: RabbitMqConstants.OrderNotificationRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        // Purge Queue truoc khi test (xoa message cu neu co)
        _channel.QueuePurge(queue: RabbitMqConstants.OrderNotificationQueueName);

        // Publish lai message sau khi purge
        _channel.BasicPublish(
            exchange: RabbitMqConstants.OrderNotificationExchangeName,
            routingKey: RabbitMqConstants.OrderNotificationRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        // Act: chay Worker consume message
        var consumer = new OrderNotificationConsumerService(
            Mock.Of<IOrderRepository>(),
            new Mock<ILogger<OrderNotificationConsumerService>>().Object);
        var worker = new OrderNotificationWorkerService(
            new Mock<ILogger<OrderNotificationWorkerService>>().Object,
            _channel,
            consumer);

        // Chay HandleDeliveryAsync mot lan de consume message
        var result = _channel.BasicGet(queue: RabbitMqConstants.OrderNotificationQueueName, autoAck: false);
        result.Should().NotBeNull("Queue phai co message can consume");
        await worker.HandleDeliveryAsync(result!, CancellationToken.None);

        // Assert: Queue trong sau khi consume (Ack)
        var queueResult = _channel.BasicGet(queue: RabbitMqConstants.OrderNotificationQueueName, autoAck: false);
        queueResult.Should().BeNull("Queue phai trong sau khi da Ack message");
    }
}
