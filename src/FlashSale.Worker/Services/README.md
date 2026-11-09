# Folder: Services (Worker)

Chứa các Background Service của Async Worker (.NET Background Service).

| File | Chức năng |
|------|-----------|
| `Worker.cs` | Background Service chính, consume message từ Queue `order_pipeline_queue` qua `BasicGet`, triển khai Retry + DLQ (FR-04, FR-05). |
| `OutboxPublisherWorkerService.cs` | Background Service publish Outbox Pending ra RabbitMQ Queue định kỳ (mỗi 30 giây) (FR-04, NFR-06). |
| `OutboxPurgeWorkerService.cs` | Background Service purge/archive bảng Outbox định kỳ (mỗi 24 giờ) (NFR-06). |
| `OrderNotificationWorkerService.cs` | Background Service consume message từ Queue `order_notification_queue` (downstream) (FR-04, NFR-06). |

## Quy tắc

- Worker là .NET Background Service, chạy độc lập từ Api.
- Consumer phải ack message sau khi xử lý thành công, nack/retry sau khi lỗi.
- Không dùng `Console.WriteLine` trong consumer.
