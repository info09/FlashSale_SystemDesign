# FlashSale.Application.Tests

Unit tests cho `FlashSale.Application`.

## Phạm vi tiêu biểu
- `OrderServiceTests`: tạo đơn, hết kho/validation, transaction outbox (mock dependencies).
- `InventoryServiceTests`, `RateLimitServiceTests`, `MetricServiceTests`
- `OutboxPurgeServiceTests`, `ReconciliationServiceTests`
- `ApplicationSmokeTest` và các test service liên quan khác.

## Chạy test
```bash
dotnet test tests/FlashSale.Application.Tests -c Release
```

## Quy tắc
- Mock toàn bộ dependency hạ tầng (`IOrderRepository`, `IInventoryRepository`, `IUnitOfWork`, ...).
- Kiểm tra hành vi orchestration, lỗi nghiệp vụ, và boundary điều kiện.
- AAA, tên test rõ ràng.
