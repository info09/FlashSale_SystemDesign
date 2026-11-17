# Entities (FlashSale.Domain)

Chứa các domain entity của hệ thống Flash Sale.

## Entity tiêu biểu
- `Order.cs`: đơn hàng được tạo/xử lý trong luồng flash sale.
- `Outbox.cs`: bản ghi sự kiện cho Transactional Outbox Pattern.
- `Inventory.cs`: trạng thái/tồn kho sản phẩm.
- `RateLimit.cs`: trạng thái giới hạn tần số.
- `User.cs`, `Role.cs`: thực thể phục vụ Identity/auth.
- `HealthResult.cs`: kết quả kiểm tra sức khỏe thành phần.
- `Metric.cs`: dữ liệu/chỉ số quan sát.
- `ReconciliationResult.cs`: kết quả đối chiếu nhất quán dữ liệu.

## Quy tắc
- Đặt tên theo `docs/domain-glossary.md`, tránh synonym (`Stock`, `Dlq`, ...).
- Giữ entity độc lập với hạ tầng và framework persistence.
