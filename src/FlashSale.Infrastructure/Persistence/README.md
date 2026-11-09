# Folder: Persistence (Infrastructure)

Chứa các repository implementation, DbContext, UnitOfWork của Infrastructure layer. Tham chiếu `FlashSale.Domain` + `FlashSale.Application`.

| File | Chức năng |
|------|-----------|
| `PostgreSQLPrimaryContext.cs` | DbContext chính của PostgreSQLPrimary (ghi Order + Outbox + User qua Identity). |
| `OrderRepository.cs` | Implementation `IOrderRepository` dùng PostgreSQLPrimary (EF Core). |
| `OutboxRepository.cs` | Implementation `IOutboxRepository`, bao gồm `PurgeProcessedOutboxesOlderThanAsync` (NFR-06). |
| `ReconciliationRepository.cs` | Implementation `IReconciliationRepository` (đối chiếu Order/Outbox, NFR-06). |
| `UnitOfWork.cs` | Implementation `IUnitOfWork`, quản lý transaction PostgreSQLPrimary (Transactional Outbox Pattern). |
| `AdminSeeder.cs` | Seed người dùng admin và vai trò khi khởi động. |
| `ApplicationUser.cs` | Entity User của ASP.NET Core Identity. |
| `AuthService.cs` | Dịch vụ xác thực (login, register) dùng UserManager + SignInManager. |
| `JwtTokenService.cs` | Dịch vụ ký JWT token cho Bearer authentication. |
| `UserRepository.cs` | Implementation `IUserRepository` (ASP.NET Core Identity). |

## Quy tắc

- Repository Implementation trong `Infrastructure/Persistence/` dùng EF Core.
- Không dùng raw SQL trừ khi có quyết định trong `docs/decisions.md`.
