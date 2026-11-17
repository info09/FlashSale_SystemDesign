# Persistence (FlashSale.Infrastructure)

Chứa DbContext, repository implementations, UnitOfWork và dịch vụ persistence liên quan.

## Thành phần chính
- `PostgreSQLPrimaryContext.cs`: EF Core DbContext cho PostgreSQL Primary (Order, Outbox, Identity).
- `OrderRepository.cs`, `OutboxRepository.cs`, `ReconciliationRepository.cs`, `UserRepository.cs`: triển khai repository.
- `UnitOfWork.cs`: điều phối transaction (ghi Order + Outbox nhất quán).
- `AuthService.cs`, `JwtTokenService.cs`: xác thực và phát hành JWT.
- `HealthCheckService.cs`, `PostgreSqlDatabaseConnectionChecker.cs`: kiểm tra sức khỏe/kết nối.
- `MetricCollector.cs`: thu thập metric từ hạ tầng (queue/db/cache tùy triển khai).
- `AdminSeeder.cs`, `ApplicationUser.cs`: Identity user/seed dữ liệu khởi tạo.

## Quy tắc
- Repository implementation dùng EF Core; tránh raw SQL trừ khi có quyết định rõ ràng.
- Không để logic nghiệp vụ phức tạp trong repository; giữ trách nhiệm truy cập dữ liệu.
