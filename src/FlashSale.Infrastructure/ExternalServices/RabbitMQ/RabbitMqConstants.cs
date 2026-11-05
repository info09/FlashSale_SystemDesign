namespace FlashSale.Infrastructure.ExternalServices.RabbitMQ;

/// <summary>
/// Các hằng số tên Exchange/Queue/Routing Key cho RabbitMQ.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RabbitMQ, Queue, OrderPipeline, Message.
/// </summary>
public static class RabbitMqConstants
{
    /// <summary>Tên Exchange cho pipeline tạo đơn.</summary>
    public const string ExchangeName = "flash_sale_exchange";

    /// <summary>Tên Queue chứa công việc tạo đơn chờ Async Worker tiêu thụ.</summary>
    public const string QueueName = "order_pipeline_queue";

    /// <summary>Routing key dùng để publish message tạo đơn vào Exchange.</summary>
    public const string RoutingKey = "order.created";

    /// <summary>Tên Dead Letter Queue cho message lỗi sau 3 lần Retry.</summary>
    public const string DeadLetterQueueName = "order_pipeline_dlq";

    /// <summary>Tên Dead Letter Exchange.</summary>
    public const string DeadLetterExchangeName = "flash_sale_dlq_exchange";

    /// <summary>Routing key cho Dead Letter Queue.</summary>
    public const string DeadLetterRoutingKey = "order.dlq";
}
