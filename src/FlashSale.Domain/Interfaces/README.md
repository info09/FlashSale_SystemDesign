# Folder: Interfaces (Domain)

Chứa các giao diện repository của Domain layer. Không chứa implementation chi tiết EF Core.

| File | Chức năng |
|------|-----------|
| `IOrderRepository.cs` | Giao diện thao tác Order trong kho dữ liệu (PostgreSQLPrimary). |
| `IOutboxRepository.cs` | Giao diện thao tác Outbox trong kho dữ liệu (PostgreSQLPrimary). |
| `IReconciliationRepository.cs` | Giao diện truy vấn OrderId và Outbox cho quy trình Reconciliation (NFR-06). |
| `IUnitOfWork.cs` | Giao diện quản lý transaction PostgreSQLPrimary (Transactional Outbox Pattern). |
| `IInventoryRepository.cs` | Giao diện thao tác Inventory trên Redis. |
| `IRateLimitRepository.cs` | Giao diện thao tác RateLimit trên Redis. |
| `IUserRepository.cs` | Giao diện thao tác User (ASP.NET Core Identity). |
| `IUserService.cs` | Giao diện dịch vụ người dùng (Identity). |
| `IJwtTokenService.cs` | Giao diện dịch vụ ký JWT token. |

## Quy tắc

- Repository Interface trong `Domain/Interfaces/` KHÔNG chứa implementation chi tiết EF Core.
- Không dùng `DbContext`, `DbSet`, Entity Framework trong Domain.
