# Folder: Scripts (Infrastructure/ExternalServices/Redis)

Chứa các file Lua Script chạy trên Redis để thực thi thao tác nguyên tử (atomic).

| File | Chức năng |
|------|-----------|
| `rate_limit_token_bucket.lua` | Script TokenBucket cho Rate Limit (FR-01, NFR-04). |
| `atomic_inventory_deduct.lua` | Script trừ kho atomic cho AtomicInventoryOffloading (FR-02, NFR-01). |

## Quy tắc

- File `.lua` đặt trong `Infrastructure/ExternalServices/Redis/Scripts/`, tên snake_case (Convention 1.1).
- Lua script phải nguyên tử (atomic), không chia thành nhiều lệnh Redis riêng lẻ.
- Không dùng Lua script cho logic phức tạp > 50 dòng.
