# FlashSale.Infrastructure.Tests

Tests cho `FlashSale.Infrastructure` (unit + integration).

## Phạm vi tiêu biểu
- **Unit:** `RedisRateLimitServiceTests`, `RedisInventoryServiceTests`, `RabbitMqPublisherServiceTests`, `AuthServiceTests`, `HealthCheckServiceTests`, `OrderCreatedMessageTests`, ...
- **Integration (Testcontainers):** kết nối/hoạt động với PostgreSQL, Redis, RabbitMQ; repositories (`OrderRepository`/`OutboxRepository`/`ReconciliationRepository`/`UserRepository`), transaction outbox, purge, publish/consume hạ tầng liên quan.

## Chạy test
```bash
dotnet test tests/FlashSale.Infrastructure.Tests -c Release
```

## Lưu ý
- Cần Docker để chạy integration tests dùng Testcontainers.
- Một số test có thể tốn thời gian do khởi tạo container.
