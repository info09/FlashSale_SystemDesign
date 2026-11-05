using FlashSale.Application.Features.Order.Commands;
using FlashSale.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FlashSale.Infrastructure.ExternalServices.RabbitMQ;

/// <summary>
/// Triển khai IRabbitMqPublisherService sử dụng RabbitMQ.Client.
/// Triển khai Publish OrderCreatedMessage vào RabbitMQ Queue (OrderPipeline).
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RabbitMQ, Queue, OrderPipeline, Message.
/// Không được gọi là: IRabbitMqClient, IRabbitMqProducer, IRabbitMqSender.
/// </summary>
public class RabbitMqPublisherService : IRabbitMqPublisherService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisherService(string connectionString)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Khai báo Exchange (durable để soprav lại khi broker restart)
        _channel.ExchangeDeclare(
            exchange: RabbitMqConstants.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Khai báo Dead Letter Exchange
        _channel.ExchangeDeclare(
            exchange: RabbitMqConstants.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Khai báo Queue chính với arguments DLX
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchangeName },
            { "x-dead-letter-routing-key", RabbitMqConstants.DeadLetterRoutingKey },
            { "x-max-length", 100000 },
            { "x-message-ttl", 86400000 } // 24 hours
        };

        _channel.QueueDeclare(
            queue: RabbitMqConstants.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        // Bind Queue vào Exchange với Routing Key
        _channel.QueueBind(
            queue: RabbitMqConstants.QueueName,
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey);

        // Khai báo Dead Letter Queue
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

    public async Task PublishOrderCreatedAsync(OrderCreatedMessage orderCreatedMessage, CancellationToken cancellationToken = default)
    {
        var messageJson = orderCreatedMessage.ToJson();
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = orderCreatedMessage.MessageId.ToString();
        properties.Timestamp = new AmqpTimestamp(orderCreatedMessage.OccurredOn.ToUnixTimeSeconds());
        properties.ContentType = "application/json";
        properties.CorrelationId = orderCreatedMessage.CorrelationId.ToString();

        _channel.BasicPublish(
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
