# Events — Inventory (Application/Features)

Chứa message contract/sự kiện liên quan luồng đặt hàng/tồn kho.

## File
- `OrderCreatedMessage.cs`: contract message khi đơn được tạo/được publish vào pipeline (bao gồm định danh, thời gian, payload cần thiết).

## Quy tắc
- Message phải có thông tin truy vết cần thiết (ví dụ `CorrelationId`, `MessageId`, `OccurredOn`) theo convention dự án.
- Không đưa secret/PII không cần thiết vào payload.
- Tên/ý nghĩa bám `docs/domain-glossary.md` (`Order`, `OrderPipeline`, `Message`).
