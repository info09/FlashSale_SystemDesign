using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using RabbitMQ.Client;
using System.Text;

namespace FlashSale.Infrastructure.ExternalServices.RabbitMQ;

/// <summary>
/// Triển khai IRabbitMqPublisherService sử dụng RabbitMQ.Client.
/// Hỗ trợ kết nối/publish lazy để không chặn khởi động ứng dụng khi broker chưa sẵn sàng.
/// </summary>
public class RabbitMqPublisherService : IRabbitMqPublisherService, IDisposable
{
    private readonly IRabbitMqConnectionProvider? _connectionProvider;
    private readonly IConnection? _fixedConnection;
    private readonly RabbitMqConnectionProvider? _ownedProvider;
    private readonly object _fixedChannelSync = new();
    private IModel? _fixedChannel;
    private bool _topologyDeclaredOnFixedChannel;
    private bool _disposed;

    public RabbitMqPublisherService(string connectionString)
    {
        _ownedProvider = new RabbitMqConnectionProvider(connectionString);
        _connectionProvider = _ownedProvider;
    }

    public RabbitMqPublisherService(IRabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
    }

    public RabbitMqPublisherService(IConnection connection)
    {
        _fixedConnection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public Task PublishOrderCreatedAsync(OrderCreatedMessage orderCreatedMessage, CancellationToken cancellationToken = default)
    {
        var messageJson = orderCreatedMessage.ToJson();
        return PublishCoreAsync(
            RabbitMqConstants.ExchangeName,
            RabbitMqConstants.RoutingKey,
            messageJson,
            orderCreatedMessage.CorrelationId,
            orderCreatedMessage.MessageId,
            orderCreatedMessage.OccurredOn,
            cancellationToken);
    }

    public Task PublishAsync(
        string exchange,
        string routingKey,
        string messageJson,
        Guid correlationId,
        Guid? messageId = null,
        CancellationToken cancellationToken = default)
    {
        return PublishCoreAsync(
            exchange,
            routingKey,
            messageJson,
            correlationId,
            messageId ?? Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    private Task PublishCoreAsync(
        string exchange,
        string routingKey,
        string messageJson,
        Guid correlationId,
        Guid messageId,
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_fixedConnection is not null)
        {
            PublishWithFixedConnection(exchange, routingKey, messageJson, correlationId, messageId, occurredOn);
            return Task.CompletedTask;
        }

        if (_connectionProvider is null)
            throw new InvalidOperationException("Không có RabbitMQ connection provider.");

        var connection = _connectionProvider.GetConnection();
        using var channel = connection.CreateModel();
        DeclareTopology(channel);
        Publish(channel, exchange, routingKey, messageJson, correlationId, messageId, occurredOn);
        return Task.CompletedTask;
    }

    private void PublishWithFixedConnection(
        string exchange,
        string routingKey,
        string messageJson,
        Guid correlationId,
        Guid messageId,
        DateTimeOffset occurredOn)
    {
        lock (_fixedChannelSync)
        {
            if (_fixedChannel is null || !_fixedChannel.IsOpen)
            {
                try { _fixedChannel?.Dispose(); } catch { /* ignore */ }
                _fixedChannel = _fixedConnection!.CreateModel();
                _topologyDeclaredOnFixedChannel = false;
            }

            if (!_topologyDeclaredOnFixedChannel)
            {
                DeclareTopology(_fixedChannel);
                _topologyDeclaredOnFixedChannel = true;
            }

            Publish(_fixedChannel, exchange, routingKey, messageJson, correlationId, messageId, occurredOn);
        }
    }

    private static void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchangeName },
            { "x-dead-letter-routing-key", RabbitMqConstants.DeadLetterRoutingKey },
            { "x-max-length", 100000 },
            { "x-message-ttl", 86400000 }
        };

        channel.QueueDeclare(
            queue: RabbitMqConstants.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        channel.QueueBind(
            queue: RabbitMqConstants.QueueName,
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey);

        channel.QueueDeclare(
            queue: RabbitMqConstants.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: RabbitMqConstants.DeadLetterQueueName,
            exchange: RabbitMqConstants.DeadLetterExchangeName,
            routingKey: RabbitMqConstants.DeadLetterRoutingKey);

        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.OrderNotificationExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: RabbitMqConstants.OrderNotificationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: RabbitMqConstants.OrderNotificationQueueName,
            exchange: RabbitMqConstants.OrderNotificationExchangeName,
            routingKey: RabbitMqConstants.OrderNotificationRoutingKey);
    }

    private static void Publish(
        IModel channel,
        string exchange,
        string routingKey,
        string messageJson,
        Guid correlationId,
        Guid messageId,
        DateTimeOffset occurredOn)
    {
        var body = Encoding.UTF8.GetBytes(messageJson);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = messageId.ToString();
        properties.Timestamp = new AmqpTimestamp(occurredOn.ToUnixTimeSeconds());
        properties.ContentType = "application/json";
        properties.CorrelationId = correlationId.ToString();

        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_fixedConnection is not null)
        {
            lock (_fixedChannelSync)
            {
                try { _fixedChannel?.Close(); } catch { /* ignore */ }
                try { _fixedChannel?.Dispose(); } catch { /* ignore */ }
                _fixedChannel = null;
            }

            try { _fixedConnection.Close(); } catch { /* ignore */ }
            try { _fixedConnection.Dispose(); } catch { /* ignore */ }
            return;
        }

        _ownedProvider?.Dispose();
    }
}
