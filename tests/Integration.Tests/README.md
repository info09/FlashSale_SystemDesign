# Integration.Tests

Test end-to-end toàn bộ luồng: ApiGateway → RabbitMQ → AsyncWorker → PostgreSQLPrimary + Outbox. Sử dụng Testcontainers (PostgreSQL, Redis, RabbitMQ).

## Cấu trúc

| Thư mục | File | Mô tả |
|---------|------|-------|
| `EndToEnd/` | `FlashSaleBuyEndToEndTest.cs` | Test luồng mua hàng (202 Accepted, Order + Outbox) |
| `EndToEnd/` | `DeadLetterQueueIntegrationTest.cs` | Test message chuyển vào DLQ sau 3 lần thất bại |
| `EndToEnd/` | `ReconciliationEndToEndTest.cs` | Test quy trình đối chiếu Order/Outbox |
| `EndToEnd/` | `OutboxPurgeApiEndToEndTest.cs` | Test endpoint purge Outbox qua HTTP |
| `EndToEnd/` | `OrderNotificationWorkerIntegrationTest.cs` | Test consume message notification |
| `EndToEnd/` | `RateLimitEndToEndTest.cs` | Test rate limit qua HTTP |
| `EndToEnd/` | `InventoryEndToEndTest.cs` | Test tồn kho qua HTTP |
| `EndToEnd/` | `AuthEndToEndTest.cs` | Test đăng ký/đăng nhập, gọi protected endpoint |
| `EndToEnd/` | `AuthFlowEndToEndTest.cs` | Test luồng auth đầy đủ |
| `Unit/` | `IntegrationSmokeTest.cs` | Test khói tích hợp |

## Cách chạy

```bash
dotnet test tests/Integration.Tests -c Release
```

Yêu cầu Docker/Testcontainers. Các test chạy Redis, PostgreSQL, RabbitMQ thật trong container.

## Lưu ý

- Một số test có thể flaky khi chạy toàn bộ suite do Testcontainers khởi động container. Nên chạy từng filter khi cần debug.
