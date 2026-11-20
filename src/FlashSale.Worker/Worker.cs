using System.Text;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain;
using FlashSale.Worker.Consumers;
using RabbitMQ.Client;

namespace FlashSale.Worker;

/// <summary>
/// Async Worker (.NET 8 Background Service) tiêu thụ message từ RabbitMQ Queue (order_pipeline_queue).
/// Trien khai Retry voi ExponentialBackoff (toi da 3 lan) khi xu ly message that bai.
/// Sau 3 lan that bai, message duoc chuyen vao DeadLetterQueue (order_pipeline_dlq) (FR-05).
/// </summary>
public class Worker : BackgroundService
{
    private const string RetryCountHeader = "x-retry-count";

    private readonly ILogger<Worker> _logger;
    private readonly IRabbitMqConnectionProvider? _connectionProvider;
    private readonly OrderCreatedConsumerService _consumer;
    private readonly ExponentialBackoffPolicy _backoffPolicy;
    private readonly object _channelSync = new();
    private IModel? _channel;

    public Worker(
        ILogger<Worker> logger,
        IRabbitMqConnectionProvider connectionProvider,
        OrderCreatedConsumerService consumer,
        ExponentialBackoffPolicy backoffPolicy)
    {
        _logger = logger;
        _connectionProvider = connectionProvider;
        _consumer = consumer;
        _backoffPolicy = backoffPolicy;
    }

    // Constructor phục vụ unit test với channel cố định.
    public Worker(
        ILogger<Worker> logger,
        IModel channel,
        OrderCreatedConsumerService consumer,
        ExponentialBackoffPolicy backoffPolicy)
    {
        _logger = logger;
        _channel = channel;
        _consumer = consumer;
        _backoffPolicy = backoffPolicy;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlashSale Worker is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var channel = EnsureChannel();
                if (channel is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                var result = channel.BasicGet(queue: RabbitMqConstants.QueueName, autoAck: false);
                if (result is not null)
                {
                    await HandleDeliveryAsync(result, stoppingToken);
                }
                else
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi vòng lặp consume order_pipeline_queue, sẽ thử lại.");
                ResetChannel();
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    internal async Task HandleDeliveryAsync(BasicGetResult result, CancellationToken stoppingToken)
    {
        var channel = GetRequiredChannel();
        try
        {
            var json = Encoding.UTF8.GetString(result.Body.ToArray());
            await _consumer.ProcessMessageAsync(json, stoppingToken);
            channel.BasicAck(result.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            var retryCount = GetRetryCount(result.BasicProperties);

            if (_backoffPolicy.CanRetry(retryCount))
            {
                var delay = _backoffPolicy.GetDelay(retryCount);
                _logger.LogWarning(ex,
                    "That bai xu ly message {MessageId}, thu lai lan {RetryCount} sau {Delay}ms.",
                    result.BasicProperties?.MessageId, retryCount + 1, delay.TotalMilliseconds);
                await Task.Delay(delay, stoppingToken);

                RepublishMessage(channel, result.Body.ToArray(), result.BasicProperties, retryCount + 1);
                channel.BasicNack(result.DeliveryTag, false, false);
            }
            else
            {
                _logger.LogError(ex,
                    "Message {MessageId} vuot qua {MaxRetryCount} lan thu lai, chuyen vao DeadLetterQueue {DeadLetterQueueName}.",
                    result.BasicProperties?.MessageId, _backoffPolicy.MaxRetryCount, RabbitMqConstants.DeadLetterQueueName);
                MoveToDeadLetterQueue(channel, result.Body.ToArray(), result.BasicProperties);
                channel.BasicAck(result.DeliveryTag, false);
            }
        }
    }

    private IModel? EnsureChannel()
    {
        lock (_channelSync)
        {
            if (_connectionProvider is null)
                return _channel;

            if (_channel is { IsOpen: true })
                return _channel;

            ResetChannelUnsafe();

            if (!_connectionProvider.TryGetConnection(out var connection) || connection is null || !connection.IsOpen)
                return null;

            var channel = connection.CreateModel();
            DeclareTopology(channel);
            _channel = channel;
            return _channel;
        }
    }

    private IModel GetRequiredChannel()
    {
        lock (_channelSync)
        {
            if (_channel is { IsOpen: true })
                return _channel;
        }

        return EnsureChannel() ?? throw new InvalidOperationException("RabbitMQ channel chưa sẵn sàng.");
    }

    private void ResetChannel()
    {
        lock (_channelSync)
        {
            ResetChannelUnsafe();
        }
    }

    private void ResetChannelUnsafe()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _channel?.Dispose(); } catch { /* ignore */ }
        _channel = null;
    }

    private static void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(RabbitMqConstants.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.ExchangeDeclare(RabbitMqConstants.DeadLetterExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);

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

        channel.QueueBind(RabbitMqConstants.QueueName, RabbitMqConstants.ExchangeName, RabbitMqConstants.RoutingKey);

        channel.QueueDeclare(RabbitMqConstants.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(RabbitMqConstants.DeadLetterQueueName, RabbitMqConstants.DeadLetterExchangeName, RabbitMqConstants.DeadLetterRoutingKey);

        channel.ExchangeDeclare(RabbitMqConstants.OrderNotificationExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(RabbitMqConstants.OrderNotificationQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(RabbitMqConstants.OrderNotificationQueueName, RabbitMqConstants.OrderNotificationExchangeName, RabbitMqConstants.OrderNotificationRoutingKey);
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers == null)
            return 0;

        if (!properties.Headers.TryGetValue(RetryCountHeader, out var value))
            return 0;

        if (value is byte[] bytes)
        {
            if (int.TryParse(Encoding.UTF8.GetString(bytes), out var parsedBytes))
                return parsedBytes;
            return 0;
        }

        if (value is int retryCount)
            return retryCount;

        if (int.TryParse(value.ToString(), out var parsedStr))
            return parsedStr;

        return 0;
    }

    private static void RepublishMessage(IModel channel, byte[] body, IBasicProperties? properties, int newRetryCount)
    {
        var newProps = channel.CreateBasicProperties();
        newProps.Persistent = properties?.Persistent ?? true;
        newProps.ContentType = properties?.ContentType ?? "application/json";
        newProps.CorrelationId = properties?.CorrelationId;
        newProps.MessageId = properties?.MessageId;

        var headers = new Dictionary<string, object>();
        if (properties?.Headers != null)
        {
            foreach (var kvp in properties.Headers)
                headers[kvp.Key] = kvp.Value;
        }
        headers[RetryCountHeader] = newRetryCount;
        newProps.Headers = headers;

        channel.BasicPublish(
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey,
            mandatory: false,
            basicProperties: newProps,
            body: body);
    }

    private static void MoveToDeadLetterQueue(IModel channel, byte[] body, IBasicProperties? properties)
    {
        var newProps = channel.CreateBasicProperties();
        newProps.Persistent = properties?.Persistent ?? true;
        newProps.ContentType = properties?.ContentType ?? "application/json";
        newProps.CorrelationId = properties?.CorrelationId;
        newProps.MessageId = properties?.MessageId;

        var headers = new Dictionary<string, object>();
        if (properties?.Headers != null)
        {
            foreach (var kvp in properties.Headers)
                headers[kvp.Key] = kvp.Value;
        }
        headers[RetryCountHeader] = GetRetryCount(properties);
        newProps.Headers = headers;

        channel.BasicPublish(
            exchange: RabbitMqConstants.DeadLetterExchangeName,
            routingKey: RabbitMqConstants.DeadLetterRoutingKey,
            mandatory: false,
            basicProperties: newProps,
            body: body);
    }
}
