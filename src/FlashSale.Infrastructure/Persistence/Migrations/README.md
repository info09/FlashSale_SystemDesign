# Folder: Migrations (Infrastructure/Persistence)

Chứa các migration file của Entity Framework Core cho PostgreSQLPrimary.

| File | Chức năng |
|------|-----------|
| `20261105005114_CreateIdentityTables.cs` | Tạo bảng User, Role (ASP.NET Core Identity, Up/Down reversible). |
| `20261105043932_CreateOrdersTable.cs` | Tạo bảng Order (Id, ProductId, Quantity, ClientId, OrderStatus, CreatedOn, Up/Down reversible). |
| `20261105082943_CreateOutbox_Table.cs` | Tạo bảng Outbox (Up/Down reversible, có index ProcessingStatus/OccurredOn). |
| `PostgreSQLPrimaryContextModelSnapshot.cs` | Snapshot model của DbContext cho EF Core. |

## Quy tắc

- Tên migration file theo format `{Timestamp}_{MucDich}.cs` (Convention 1.1).
- Mỗi migration phải có Up/Down reversible.
