# Folder: Events (Inventory)

Chứa các message contract (sự kiện) của tính năng Kho hàng (Inventory) trong Application layer.

| File | Chức năng |
|------|-----------|
| `OrderCreatedMessage.cs` | Message contract cho sự kiện Order được tạo thành công (có CorrelationId, MessageId, OccurredOn, Payload). |

## Quy tắc

- Mỗi message phải có: `CorrelationId` (Guid), `MessageId` (Guid), `OccurredOn` (DateTimeOffset), `Payload` (object).
- Không chứa secret, PII, mật khẩu trong message payload.
