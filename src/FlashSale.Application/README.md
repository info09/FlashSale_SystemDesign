# FlashSale.Application

Layer Application: điều phối use-case nghiệp vụ, command/validator/event theo feature, định nghĩa abstraction cho hạ tầng. Chỉ phụ thuộc `FlashSale.Domain`.

## Thành phần chính
- `Interfaces/`: contract dịch vụ/repository integration (`IOrderService`, `IInventoryService`, `IRateLimitService`, `IOutboxService`, `IOutboxPublisher`, `IOutboxPurgeService`, `IReconciliationService`, `IRabbitMqPublisherService`, `IMetricService`, `IMetricCollector`, ...)
- `Services/`: triển khai nghiệp vụ/orchestration
  - `OrderService`: tạo Order + ghi Outbox trong transaction (Transactional Outbox Pattern)
  - `InventoryService`, `RateLimitService`
  - `OutboxService`, `OutboxPublisher`, `OutboxPurgeService`, `OutboxPurgeOptions`
  - `ReconciliationService`, `ExponentialBackoffPolicy`, `MetricService`
- `Features/`: tổ chức theo module `Auth`, `Inventory`, `RateLimit`
  - `Commands/`, `CommandHandlers/`, `Validators/`, `Events/` (tùy module)
- `DependencyInjection.cs`: đăng ký service/validator của Application

## Quy tắc
- Không phụ thuộc trực tiếp EF Core DbContext, Redis client, RabbitMQ client; làm việc qua interface.
- Logic nghiệp vụ cốt lõi ưu tiên đặt ở Domain/service phù hợp; handler/service tầng này tập trung orchestrate.
- Validator dùng FluentValidation, tên/thuật ngữ theo `docs/domain-glossary.md`.

## Test
```bash
dotnet test tests/FlashSale.Application.Tests -c Release
```
