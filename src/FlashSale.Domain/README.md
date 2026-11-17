# FlashSale.Domain

Layer Domain: mô hình nghiệp vụ cốt lõi, exception, interface trừu tượng. Không phụ thuộc layer khác và không dùng EF Core/Redis/RabbitMQ trực tiếp.

## Thành phần
- `Entities/`: thực thể nghiệp vụ như `Order`, `Outbox`, `Inventory`, `RateLimit`, `User`, `Role`, `HealthResult`, `Metric`, `ReconciliationResult`, ...
- `Interfaces/`: abstraction cho repository/unit of work/dịch vụ hạ tầng cần thiết (`IOrderRepository`, `IOutboxRepository`, `IInventoryRepository`, `IRateLimitRepository`, `IUserRepository`, `IUnitOfWork`, `IReconciliationRepository`, `IDatabaseConnectionChecker`, ...)
- `Exceptions/`: `InsufficientInventoryException`, `RateLimitExceededException`, `DeadLetterQueueException`, ...
- `RabbitMqConstants.cs`: hằng số Exchange/Queue/Routing Key chuẩn dùng xuyên solution

## Quy tắc
- Tên và ngữ nghĩa phải tuân thủ `docs/domain-glossary.md`.
- Entity tập trung bất biến/nghiệp vụ; không chứa phụ thuộc hạ tầng.
- Interface không lộ chi tiết triển khai (EF, driver cụ thể).

## Test
```bash
dotnet test tests/FlashSale.Domain.Tests -c Release
```
