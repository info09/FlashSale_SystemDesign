# Redis (FlashSale.Infrastructure/ExternalServices)

Triển khai tích hợp Redis cho RateLimit và Inventory bằng thao tác nguyên tử.

## Thành phần
- `RedisRateLimitService.cs`: kiểm tra RateLimit (Token Bucket) qua Lua script.
- `RedisInventoryService.cs`: trừ/thêm/đọc tồn kho atomic qua Lua script.
- `RedisScriptExecutor.cs`, `IRedisScriptExecutor.cs`: thực thi script Redis.
- `Scripts/`: Lua scripts
  - `rate_limit_token_bucket.lua`
  - `atomic_inventory_deduct.lua`

## Quy tắc
- Dùng `StackExchange.Redis` và thao tác atomic khi cạnh tranh cao.
- Không chia logic cần atomic thành nhiều lệnh Redis rời rạc nếu script đáp ứng được.
- Cấu hình kết nối Redis lấy từ environment/config.
