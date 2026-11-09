# FlashSale.Application.Tests

Unit test cho layer Application.

## Cấu trúc

| File | Mô tả |
|------|-------|
| `Unit/OrderServiceTests.cs` | Test OrderService (tạo đơn, hết kho, validation) |
| `Unit/ReconciliationServiceTests.cs` | Test ReconciliationService (đối chiếu Order/Outbox) |
| `Unit/OutboxPurgeServiceTests.cs` | Test OutboxPurgeService (xóa Outbox cũ) |

## Cách chạy

```bash
dotnet test tests/FlashSale.Application.Tests -c Release
```

## Quy tắc test

- Dùng Moq mock tất cả dependency external (IOrderRepository, IInventoryRepository, IOutboxService, IUnitOfWork).
- Mỗi test method tuân theo AAA (Arrange-Act-Assert).
- Tên test method: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`.
