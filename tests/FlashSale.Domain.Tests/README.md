# FlashSale.Domain.Tests

Unit tests cho `FlashSale.Domain`.

## Phạm vi
- Kiểm thử entity, invariant, exception và hành vi domain (`Order`, `Outbox`, `Inventory`, exceptions, ...).

## Chạy test
```bash
dotnet test tests/FlashSale.Domain.Tests -c Release
```

## Quy tắc
- Test nhanh, không phụ thuộc hạ tầng ngoài.
- Đặt tên theo `{MethodUnderTest}_{Scenario}_{ExpectedResult}` khi phù hợp.
- Tuân theo AAA (Arrange-Act-Assert).
