# FlashSale.Domain

Layer Domain của hệ thống Flash Sale. Không tham chiếu bất kỳ layer nào khác. Nguồn sự thật duy nhất về ngôn ngữ chung (Ubiquitous Language).

## Cấu trúc

| Thư mục | Mô tả |
|---------|-------|
| `Entities/` | Order, Outbox, ReconciliationResult |
| `Exceptions/` | DeadLetterQueueException, InsufficientInventoryException, RateLimitExceededException |
| `Interfaces/` | IOrderRepository, IOutboxRepository, IReconciliationRepository, IUnitOfWork |
| `RabbitMqConstants.cs` | Tên Exchange/Queue/Routing Key chuẩn (dùng cho cả Application và Infrastructure) |

## Quy tắc

- Entity KHÔNG chứa thuộc tính navigation phức tạp.
- Repository Interface KHÔNG chứa implementation chi tiết EF Core.
- Không dùng `DbContext`, `DbSet`, Entity Framework trong Domain.
- Tên class, exception, repository phải trùng thuật ngữ trong `docs/domain-glossary.md`.

## Cách chạy test

```bash
dotnet test tests/FlashSale.Domain.Tests -c Release
```
