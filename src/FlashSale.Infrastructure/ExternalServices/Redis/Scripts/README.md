# Redis Scripts (FlashSale.Infrastructure/ExternalServices/Redis)

Chứa Lua scripts chạy trên Redis để đảm bảo thao tác nguyên tử.

## Scripts
- `rate_limit_token_bucket.lua`: thuật toán Token Bucket cho RateLimit.
- `atomic_inventory_deduct.lua`: trừ tồn kho nguyên tử, ngăn over-selling.

## Quy tắc
- Script phải atomic và dễ kiểm thử.
- Tên file snake_case, đúng mục đích nghiệp vụ.
- Tránh nhúng logic quá phức tạp/dài; thay đổi script cần cập nhật test liên quan.
