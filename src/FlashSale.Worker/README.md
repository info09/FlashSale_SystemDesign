# FlashSale.Worker

Async Worker (.NET Background Service) của hệ thống Flash Sale. Tham chiếu `FlashSale.Application` + `FlashSale.Infrastructure`. Chạy độc lập từ API.

## Cấu trúc

| Thư mục | Mô tả |
|---------|-------|
| `Consumers/` | OrderCreatedConsumerService, OrderNotificationConsumerService |
| `Services/` | Worker, OutboxPurgeWorkerService, OutboxPublisherWorkerService |
| `Program.cs` | Cấu hình host, RabbitMQ connection/channel, DI, BackgroundService |

## Background Service

| Service | Mô tả | Tần suất |
|---------|-------|----------|
| `Worker` | Consume message từ `order_pipeline_queue`, tạo Order + ghi Outbox | Liên tục |
| `OutboxPublisherWorkerService` | Publish Outbox Pending ra Order Notification Queue | Mỗi 30 giây |
| `OutboxPurgeWorkerService` | Purge Outbox đã xử lý cũ hơn PurgeDays | Mỗi 24 giờ |
| `OrderNotificationWorkerService` | Consume message từ `order_notification_queue` | Liên tục |

## Cách chạy

```bash
dotnet run --project src/FlashSale.Worker/FlashSale.Worker.csproj
```

Cần cấu hình biến môi trường: `POSTGRESQL_PRIMARY_CONNECTION_STRING`, `RABBITMQ_CONNECTION_STRING`, v.v. (xem `.env.example`).

## Cách chạy test

```bash
dotnet test tests/FlashSale.Worker.Tests -c Release
```
