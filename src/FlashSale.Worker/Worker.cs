using FlashSale.Application.Services;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Worker.Consumers;
using RabbitMQ.Client;
using System.Text;

namespace FlashSale.Worker;

/// <summary>
/// Async Worker (.NET 8 Background Service) tieu thu message tu RabbitMQ Queue (order_pipeline_queue).
/// Moi message duoc xu ly: tao Order + ghi Outbox trong cung 1 transaction PostgreSQLPrimary (Transactional Outbox Pattern).
/// Trien khai Retry voi ExponentialBackoff (toi da 3 lan) khi xu ly message that bai.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: AsyncWorker, Queue, Message, Order, Outbox, PostgreSQLPrimary, TransactionalOutboxPattern, ExponentialBackoff, Retry, DeadLetterQueue.
/// </summary>
public class Worker : BackgroundService
{
    private const string RetryCountHeader = "x-retry-count";

    private readonly ILogger<Worker> _logger;
    private readonly IModel _channel;
    private readonly OrderCreatedConsumerService _consumer;
    private readonly ExponentialBackoffPolicy _backoffPolicy;

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

        // Khai bao Queue neu chua ton tai (cung arguments voi RabbitMqPublisherService de tranh PRECONDITION_FAILED)
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

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _channel.BasicGet(queue: RabbitMqConstants.QueueName, autoAck: false);
            if (result is not null)
            {
                try
                {
                    var json = Encoding.UTF8.GetString(result.Body.ToArray());
                    await _consumer.ProcessMessageAsync(json, stoppingToken);
                    _channel.BasicAck(result.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    var retryCount = GetRetryCount(result.BasicProperties);

                    if (_backoffPolicy.CanRetry(retryCount))
                    {
                        // Doi theo Exponential Backoff truoc khi gui lai message
                        var delay = _backoffPolicy.GetDelay(retryCount);
                        _logger.LogWarning(ex,
                            "That bai xu ly message {MessageId}, thu lai lan {RetryCount} sau {Delay}ms.",
                            result.BasicProperties?.MessageId, retryCount + 1, delay.TotalMilliseconds);
                        await Task.Delay(delay, stoppingToken);

                        // Gui lai message voi retry count tang len
                        RepublishMessage(result.Body.ToArray(), result.BasicProperties, retryCount + 1);

                        // Xoa message cu khoi queue (khong requeue, vi da gui lai tren)
                        _channel.BasicNack(result.DeliveryTag, false, false);
                    }
                    else
                    {
                        // Vuot qua so lan thu lai toi da, gui den DLQ qua Dead Letter Exchange
                        _logger.LogError(ex,
                            "Message {MessageId} vuot qua {MaxRetryCount} lan thu lai, gui den DLQ.",
                            result.BasicProperties?.MessageId, _backoffPolicy.MaxRetryCount);
                        _channel.BasicNack(result.DeliveryTag, false, false);
                    }
                }
            }
            else
            {
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers == null)
            return 0;

        if (!properties.Headers.TryGetValue(RetryCountHeader, out var value))
            return 0;

        // RabbitMQ luu header nhu byte[], string, hoac int tuong ung
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

    private void RepublishMessage(byte[] body, IBasicProperties? properties, int newRetryCount)
    {
        var newProps = _channel.CreateBasicProperties();
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

        _channel.BasicPublish(
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: RabbitMqConstants.RoutingKey,
            mandatory: false,
            basicProperties: newProps,
            body: body);
    }
}
