using FlashSale.Application.Features.Order.Commands;

namespace FlashSale.Infrastructure.ExternalServices.RabbitMQ;

/// <summary>
/// Giao diện dịch vụ publish MessageOrderCreated vào RabbitMQ Queue.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RabbitMQ, Queue, OrderPipeline, Message.
/// Không được gọi là: IMessagePublisher, IMessageBroker, IRabbitMqClient.
/// </summary>
public interface IRabbitMqPublisherService
{
    /// <summary>
    /// Publish message tạo đơn vào RabbitMQ Queue.
    /// </summary>
    /// <param name="orderCreatedMessage">Thông tin đơn hàng đã tạo.</param>
    /// <param name="cancellationToken">Token hủy bỏ thao tác bất đồng bộ.</param>
    Task PublishOrderCreatedAsync(OrderCreatedMessage orderCreatedMessage, CancellationToken cancellationToken = default);
}
