# Folder: Entities (Domain)

Chứa các entity nghiệp vụ (domain entity) của hệ thống Flash Sale.

| File | Chức năng |
|------|-----------|
| `Order.cs` | Entity đơn hàng (Order), lưu thông tin đơn hàng trong PostgreSQLPrimary. |
| `Outbox.cs` | Entity Outbox, lưu sự kiện trong Transactional Outbox Pattern (FR-04, NFR-06). |
| `Inventory.cs` | Entity tồn kho, lưu trạng thái inventory trên Redis. |
| `RateLimit.cs` | Entity trạng thái rate limit của client trên Redis. |
| `User.cs` | Entity người dùng (ASP.NET Core Identity). |
| `Role.cs` | Entity vai trò (ASP.NET Core Identity). |
| `ReconciliationResult.cs` | Kết quả quy trình Reconciliation (đối chiếu Order/Outbox, NFR-06). |

## Quy tắc

- Entity KHÔNG chứa thuộc tính navigation phức tạp.
- Tên entity phải trùng thuật ngữ chuẩn trong `docs/domain-glossary.md`.
