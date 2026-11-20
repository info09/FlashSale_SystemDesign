using FlashSale.Application.Features.Inventory.Events;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien dich vu publish MessageOrderCreated vao RabbitMQ Queue.
/// Thuat ngua chuan theo docs/domain-glossary.md: RabbitMQ, Queue, OrderPipeline, Message.
/// Khong duoc goi la: IMessagePublisher, IMessageBroker, IRabbitMqClient.
/// </summary>
public interface IRabbitMqPublisherService
{
    /// <summary>
    /// Publish message tao don vao RabbitMQ Queue.
    /// </summary>
    /// <param name="orderCreatedMessage">Thong tin don hang da tao.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task PublishOrderCreatedAsync(OrderCreatedMessage orderCreatedMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish mot message tuych chinh vao Exchange/Routing Key bat ki.
    /// </summary>
    /// <param name="exchange">Ten Exchange dich.</param>
    /// <param name="routingKey">Routing key dich.</param>
    /// <param name="messageJson">Noi dung message dang JSON.</param>
    /// <param name="correlationId">CorrelationId lien ket truy xuat.</param>
    /// <param name="messageId">Ma dinh danh message.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task PublishAsync(
        string exchange,
        string routingKey,
        string messageJson,
        Guid correlationId,
        Guid? messageId = null,
        CancellationToken cancellationToken = default);
}
