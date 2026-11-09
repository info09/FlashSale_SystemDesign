using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using FlashSale.Worker.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace FlashSale.Worker.Services;

/// <summary>
/// Background Service tieu thu message OrderNotification tu RabbitMQ Queue (order_notification_queue).
/// Chay doc lap tu Worker chinh (order_pipeline_queue), dam bao su kien Outbox da xuat phat duoc tien tiep xu ly downstream.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: OrderNotificationWorkerService, AsyncWorker, Queue, Message, OrderNotification, RabbitMQ.
/// </summary>
public class OrderNotificationWorkerService : BackgroundService
{
    private const string RetryCountHeader = "x-retry-count";

    private readonly ILogger<OrderNotificationWorkerService> _logger;
    private readonly IModel _channel;
    private readonly OrderNotificationConsumerService _consumer;

    public OrderNotificationWorkerService(
        ILogger<OrderNotificationWorkerService> logger,
        IModel channel,
        OrderNotificationConsumerService consumer)
    {
        _logger = logger;
        _channel = channel;
        _consumer = consumer;
    }

    /// <summary>
    /// Vong lap chinh cua Background Service: doc message tu Queue order_notification_queue qua BasicGet.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderNotificationWorkerService is running.");

        // Khai bao Queue neu chua ton tai
        _channel.QueueDeclare(
            queue: RabbitMqConstants.OrderNotificationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _channel.BasicGet(queue: RabbitMqConstants.OrderNotificationQueueName, autoAck: false);
            if (result is not null)
            {
                await HandleDeliveryAsync(result, stoppingToken);
            }
            else
            {
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Xu ly mot delivery duoc lay tu Queue order_notification_queue.
    /// </summary>
    internal async Task HandleDeliveryAsync(BasicGetResult result, CancellationToken stoppingToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(result.Body.ToArray());
            await _consumer.ProcessMessageAsync(json, stoppingToken);
            _channel.BasicAck(result.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi xu ly message notification {MessageId} tu Queue {Queue}.",
                result.BasicProperties?.MessageId, RabbitMqConstants.OrderNotificationQueueName);
            _channel.BasicNack(result.DeliveryTag, false, false);
        }
    }
}
