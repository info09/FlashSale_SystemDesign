using System.Text;
using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using FlashSale.Worker.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FlashSale.Worker.Services;

/// <summary>
/// Background Service tiêu thụ message OrderNotification từ RabbitMQ Queue (order_notification_queue).
/// </summary>
public class OrderNotificationWorkerService : BackgroundService
{
    private readonly ILogger<OrderNotificationWorkerService> _logger;
    private readonly IRabbitMqConnectionProvider? _connectionProvider;
    private readonly OrderNotificationConsumerService _consumer;
    private readonly object _channelSync = new();
    private IModel? _channel;

    public OrderNotificationWorkerService(
        ILogger<OrderNotificationWorkerService> logger,
        IRabbitMqConnectionProvider connectionProvider,
        OrderNotificationConsumerService consumer)
    {
        _logger = logger;
        _connectionProvider = connectionProvider;
        _consumer = consumer;
    }

    // Constructor phục vụ unit test với channel cố định.
    public OrderNotificationWorkerService(
        ILogger<OrderNotificationWorkerService> logger,
        IModel channel,
        OrderNotificationConsumerService consumer)
    {
        _logger = logger;
        _channel = channel;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderNotificationWorkerService is running.");

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

                var result = channel.BasicGet(queue: RabbitMqConstants.OrderNotificationQueueName, autoAck: false);
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
                _logger.LogWarning(ex, "Lỗi vòng lặp consume order_notification_queue, sẽ thử lại.");
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
            _logger.LogError(ex, "Loi xu ly message notification {MessageId} tu Queue {Queue}.",
                result.BasicProperties?.MessageId, RabbitMqConstants.OrderNotificationQueueName);
            channel.BasicNack(result.DeliveryTag, false, false);
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
            channel.QueueDeclare(
                queue: RabbitMqConstants.OrderNotificationQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
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
}
