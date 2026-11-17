# Migrations (FlashSale.Infrastructure/Persistence)

Chứa EF Core migrations cho `PostgreSQLPrimaryContext`.

## Migrations hiện có
- `20261105005114_CreateIdentityTables.*`: tạo bảng Identity (User/Role và liên quan).
- `20261105043932_CreateOrdersTable.*`: tạo bảng `Order`.
- `20261105082943_CreateOutbox_Table.*`: tạo bảng `Outbox` (kèm index phục vụ xử lý/purge theo thiết kế).
- `PostgreSQLPrimaryContextModelSnapshot.cs`: snapshot model hiện tại.

## Quy tắc
- Tên migration theo format `{Timestamp}_{Purpose}`.
- Mỗi migration cần có `Up/Down` reversible.
- Thay đổi schema phải đồng bộ với entity/config và được kiểm thử phù hợp.
