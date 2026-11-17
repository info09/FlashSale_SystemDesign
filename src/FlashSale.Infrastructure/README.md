# FlashSale.Infrastructure

Layer Infrastructure: triển khai truy cập dữ liệu và tích hợp hệ thống ngoài (PostgreSQL, Redis, RabbitMQ, Identity/JWT). Phụ thuộc `FlashSale.Domain` + `FlashSale.Application`.

## Thành phần
- `Persistence/`
  - `PostgreSQLPrimaryContext.cs`, repositories (`OrderRepository`, `OutboxRepository`, `UserRepository`, `ReconciliationRepository`), `UnitOfWork`
  - `AuthService.cs`, `JwtTokenService.cs`, `HealthCheckService.cs`, `MetricCollector.cs`, `AdminSeeder.cs`, `ApplicationUser.cs`
  - `Migrations/`: EF Core migrations cho Identity, Orders, Outbox
- `ExternalServices/Redis/`
  - `RedisRateLimitService.cs`, `RedisInventoryService.cs`, `RedisScriptExecutor.cs`, `IRedisScriptExecutor.cs`, `Scripts/*.lua`
- `ExternalServices/RabbitMQ/`
  - `RabbitMqPublisherService.cs`
- `DependencyInjection.cs`: đăng ký DbContext/Identity, Redis, RabbitMQ, repositories, infrastructure services

## Quy tắc
- Mọi secret/cấu hình lấy từ environment/config, không hardcode.
- Truy cập Redis dùng Lua script nguyên tử khi cần đảm bảo tính đúng trong cạnh tranh.
- Tên bảng/queue/exchange/routing key theo `docs/domain-glossary.md`.

## Test
```bash
dotnet test tests/FlashSale.Infrastructure.Tests -c Release
```
(Thường cần Docker cho Testcontainers: PostgreSQL/Redis/RabbitMQ.)
