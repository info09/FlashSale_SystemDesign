# Integration.Tests

End-to-end/integration tests xuyên suốt hệ thống Flash Sale, thường dùng Testcontainers (PostgreSQL, Redis, RabbitMQ) và/hoặc `WebApplicationFactory`.

## Phạm vi tiêu biểu (`EndToEnd/`)
- `AuthEndToEndTest`, `AuthFlowEndToEndTest`: đăng ký/đăng nhập, truy cập endpoint bảo vệ.
- `RateLimitEndToEndTest`, `InventoryEndToEndTest`: kiểm tra rate limit và tồn kho qua HTTP/Redis.
- `FlashSaleBuyEndToEndTest`: luồng mua hàng fast-path → publish/consume → ghi Order/Outbox.
- `DeadLetterQueueIntegrationTest`: chuyển message vào DLQ sau retry thất bại.
- `OutboxPurgeEndToEndTest`, `OutboxPurgeApiEndToEndTest`, `ReconciliationEndToEndTest`
- `OrderNotificationWorkerIntegrationTest`, `MetricsControllerIntegrationTest`, `HealthCheckIntegrationTest`
- `Unit/IntegrationSmokeTest.cs`

## Chạy test
```bash
dotnet test tests/Integration.Tests -c Release
```

## Lưu ý
- Yêu cầu Docker/Testcontainers; thời gian chạy dài hơn unit tests.
- Khi debug suite lớn, có thể chạy theo filter từng test/class để giảm nhiễu/flakiness do vòng đời container.
