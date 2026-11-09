# FlashSale.Infrastructure.Tests

Integration test cho layer Infrastructure. Sử dụng Testcontainers (PostgreSQL, Redis, RabbitMQ).

## Cấu trúc

| Thư mục | File | Mô tả |
|---------|------|-------|
| `Integration/` | `OutboxRepositoryIntegrationTest.cs` | Test ghi Order + Outbox cùng transaction |
| `Integration/` | `OutboxPurgeIntegrationTest.cs` | Test purge Outbox cũ |
| `Integration/` | `ReconciliationRepositoryIntegrationTest.cs` | Test truy vấn Reconciliation |
| `Integration/` | `RabbitMqPublisherIntegrationTest.cs` | Test publish message RabbitMQ thật |
| `Integration/` | `RedisInventoryIntegrationTest.cs` | Test trừ kho atomic Redis |
| `Integration/` | `RedisRateLimitIntegrationTest.cs` | Test rate limit Redis |
| `Integration/` | `UserRepositoryIntegrationTest.cs` | Test UserRepository |
| `Integration/` | `InfrastructureConnectionTests.cs` | Test kết nối dịch vụ |
| `Unit/` | `RedisRateLimitServiceTests.cs` | Test unit RedisRateLimitService |
| `Unit/` | `RedisInventoryServiceTests.cs` | Test unit RedisInventoryService |
| `Unit/` | `RabbitMqPublisherServiceTests.cs` | Test unit RabbitMqPublisherService |
| `Unit/` | `AuthServiceTests.cs` | Test unit AuthService |
| `Unit/` | `OrderCreatedMessageTests.cs` | Test message contract |

## Cách chạy

```bash
dotnet test tests/FlashSale.Infrastructure.Tests -c Release
```

Yêu cầu Docker/Testcontainers.
