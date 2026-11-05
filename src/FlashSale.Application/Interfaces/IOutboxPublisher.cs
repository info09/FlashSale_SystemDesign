namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien dich xuat phat (publish) cac su kien da luu trong bang Outbox ra Message Broker (RabbitMQ).
/// Dung trong Transactional Outbox Pattern (FR-04, NFR-06): chot du lieu vao Outbox, sau do xuat phat ra Queue.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, IOutboxPublisher, RabbitMQ, Queue, OrderPipeline, Message.
/// Khong duoc goi la: IOutboxProducer, IOutboxSender, IOutboxDispatcher.
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Xuat phat tat ca cac ghi Outbox chua duoc xu ly ra RabbitMQ Queue (OrderPipeline).
    /// Sau khi xuat phat thanh cong, danh dau ghi Outbox da duoc xu ly.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task PublishPendingOutboxAsync(CancellationToken cancellationToken = default);
}
