# Consumers (FlashSale.Worker)

Chứa các consumer xử lý message từ RabbitMQ.

## Consumers
- `OrderCreatedConsumerService.cs`: nhận message `order.created`/pipeline, tạo `Order` và ghi `Outbox` trong cùng transaction PostgreSQL (idempotent theo thiết kế hiện tại), phục vụ FR-04/NFR-06.
- `OrderNotificationConsumerService.cs`: nhận message từ `order_notification_queue` để xử lý downstream (ví dụ kiểm tra/tác vụ thông báo), thiết kế idempotent.

## Quy tắc
- Consumer điều phối xử lý qua Application/Domain abstractions, không nhúng logic hạ tầng rải rác.
- Ack/nack/retry phải nhất quán với chính sách Worker/DLQ.
- Logging có cấu trúc, không dùng `Console.WriteLine`.
