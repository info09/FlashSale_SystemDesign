# FlashSale.Infrastructure

Layer Infrastructure của hệ thống Flash Sale. Tham chiếu `FlashSale.Domain` + `FlashSale.Application`.

## Cấu trúc

| Thư mục | Mô tả |
|---------|-------|
| `Persistence/` | PostgreSQLPrimaryContext, OrderRepository, OutboxRepository, ReconciliationRepository, UnitOfWork, Migrations |
| `ExternalServices/RabbitMQ/` | RabbitMqPublisherService |
| `ExternalServices/Redis/` | RedisRateLimitService, RedisInventoryService, RedisScriptExecutor, Scripts |
| `DependencyInjection.cs` | Đăng ký dịch vụ Infrastructure |

## Các repository chính

- `OrderRepository`: thao tác bảng Order (PostgreSQLPrimary).
- `OutboxRepository`: thao tác bảng Outbox, bao gồm `PurgeProcessedOutboxesOlderThanAsync`.
- `ReconciliationRepository`: truy vấn OrderId và Outbox cho quy trình Reconciliation.
- `UnitOfWork`: quản lý transaction PostgreSQLPrimary (Transactional Outbox Pattern).

## Cách chạy test

```bash
dotnet test tests/FlashSale.Infrastructure.Tests -c Release
```

Yêu cầu Docker/Testcontainers cho PostgreSQL, Redis, RabbitMQ.
