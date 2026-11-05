using System.Text;
using System.Text.Json;
using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;
using FlashSale.Application.Features.Inventory.Events;

namespace FlashSale.Infrastructure.Tests.Integration;

public class RabbitMqPublisherIntegrationTest : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private IConnection _connection = null!;
    private IModel _channel = null!;
    private RabbitMqPublisherService _publisher = null!;

    public RabbitMqPublisherIntegrationTest()
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

        _publisher = new RabbitMqPublisherService(_connection);
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

    private static (byte[] Body, IBasicProperties Properties)? WaitForMessage(IModel channel, string queueName, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = channel.BasicGet(queue: queueName, autoAck: true);
            if (result is not null)
                return (result.Body.ToArray(), result.BasicProperties);
            Thread.Sleep(50);
        }
        return null;
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_ShouldPublishMessageToQueue_MessageCanBeConsumed()
    {
        // Arrange
        var order = new Order("product-1", 5, "client-1");
        order.UpdateStatus("Created");
        var correlationId = Guid.NewGuid();
        var message = OrderCreatedMessage.Create(order, correlationId);

        // Act
        await _publisher.PublishOrderCreatedAsync(message);

        // Assert: Lay message tu queue qua BasicGet (doi cho den khi den)
        var result = WaitForMessage(_channel, RabbitMqConstants.QueueName, TimeSpan.FromSeconds(5));
        result.Should().NotBeNull();

        var bodyJson = Encoding.UTF8.GetString(result.Value.Body);
        var bodyDoc = JsonDocument.Parse(bodyJson);
        bodyDoc.RootElement.GetProperty("CorrelationId").GetGuid().Should().Be(correlationId);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("productId").GetString().Should().Be("product-1");
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("quantity").GetInt32().Should().Be(5);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("clientId").GetString().Should().Be("client-1");
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("orderStatus").GetString().Should().Be("Created");
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_ShouldSetMessageProperties_ContentTypeAndCorrelationId()
    {
        // Arrange
        var order = new Order("product-2", 3, "client-2");
        order.UpdateStatus("Created");
        var correlationId = Guid.NewGuid();
        var message = OrderCreatedMessage.Create(order, correlationId);

        // Act
        await _publisher.PublishOrderCreatedAsync(message);

        // Assert: Lay message tu queue va kiem tra properties
        var result = WaitForMessage(_channel, RabbitMqConstants.QueueName, TimeSpan.FromSeconds(5));
        result.Should().NotBeNull();
        result.Value.Properties.ContentType.Should().Be("application/json");
        result.Value.Properties.CorrelationId.Should().Be(correlationId.ToString());
        result.Value.Properties.MessageId.Should().Be(message.MessageId.ToString());
        result.Value.Properties.Persistent.Should().BeTrue();
    }
}
