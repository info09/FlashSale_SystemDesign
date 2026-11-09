# Folder: Consumers (Worker)

Chứa các Consumer của Async Worker (.NET Background Service).

| File | Chức năng |
|------|-----------|
| `OrderCreatedConsumerService.cs` | Consume message `OrderCreated` từ Queue `order_pipeline_queue`, tạo Order + ghi Outbox trong cùng 1 transaction PostgreSQLPrimary (FR-04, NFR-06). |
| `OrderNotificationConsumerService.cs` | Consumer downstream consume message từ Queue `order_notification_queue`, kiểm tra Order tồn tại (idempotent), ghi log (NFR-06, FR-04). |

## Quy tắc

- Consumer chỉ nhận message, gọi Application Service, không chứa business logic phức tạp.
- Consumer phải ack message sau khi xử lý thành công, nack/retry sau khi lỗi.
