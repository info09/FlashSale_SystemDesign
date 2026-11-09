# Folder: RabbitMQ (Infrastructure/ExternalServices)

Chứa dịch vụ publish message vào RabbitMQ.

| File | Chức năng |
|------|-----------|
| `RabbitMqPublisherService.cs` | Implementation `IRabbitMqPublisherService`, publish OrderCreatedMessage và message tuych chinh vào RabbitMQ Exchange/Queue. Khai báo Order Notification Exchange/Queue (downstream). |

## Quy tắc

- Dùng `RabbitMQ.Client` trực tiếp (theo quyết định trong `docs/decisions.md`).
- Không dùng MassTransit trong Phase 4 MVP.
