# Folder: Redis (Infrastructure/ExternalServices)

Chứa các dịch vụ tương tác với Redis (RateLimit, Inventory, Lua Script).

| File | Chức năng |
|------|-----------|
| `RedisRateLimitService.cs` | Implementation `IRateLimitRepository`, kiểm tra rate limit qua Redis Lua Script TokenBucket (FR-01). |
| `RedisInventoryService.cs` | Dịch vụ trừ kho atomic trên Redis qua Lua Script (FR-02). |
| `RedisScriptExecutor.cs` | Thực thi Lua Script trên Redis (abstraction qua StackExchange.Redis). |
| `IRedisScriptExecutor.cs` | Giao diện thực thi Lua Script trên Redis. |
| `Scripts/` | Chứa các file `.lua` (rate_limit_token_bucket.lua, atomic_inventory_deduct.lua). |

## Quy tắc

- Dùng `StackExchange.Redis` cho Redis (theo Convention 3.14).
- Lua script phải nguyên tử (atomic), không chia thành nhiều lệnh Redis riêng lẻ.
