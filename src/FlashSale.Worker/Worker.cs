using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Worker.Consumers;

namespace FlashSale.Worker;

/// <summary>
/// Async Worker (.NET 8 Background Service) tieu thu message tu RabbitMQ Queue (order_pipeline_queue).
/// Moi message duoc xu ly: tao Order + ghi Outbox trong cung 1 transaction PostgreSQLPrimary (Transactional Outbox Pattern).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: AsyncWorker, Queue, Message, Order, Outbox, PostgreSQLPrimary, TransactionalOutboxPattern.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IModel _channel;
    private readonly OrderCreatedConsumerService _consumer;

    public Worker(ILogger<Worker> logger, IModel channel, OrderCreatedConsumerService consumer)
    {
        _logger = logger;
        _channel = channel;
        _consumer = consumer;
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
                    _logger.LogError(ex, "Khong xu ly duoc message tu Queue {Queue}.", RabbitMqConstants.QueueName);
                    _channel.BasicNack(result.DeliveryTag, false, true);
                }
            }
            else
            {
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
