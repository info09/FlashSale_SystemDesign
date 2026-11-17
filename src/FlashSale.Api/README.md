# FlashSale.Api

HTTP API của hệ thống Flash Sale. Nhận request, xác thực JWT, áp rate limit, xử lý mua hàng/tồn kho, health/metrics, và endpoint quản trị Outbox.

## Phụ thuộc
- `FlashSale.Application`
- `FlashSale.Infrastructure`

## Thành phần chính
- `Controllers/`
  - `AuthController.cs`: đăng ký/đăng nhập (`/api/v1/auth/...`)
  - `FlashSaleController.cs`: mua hàng fast-path (`POST /api/v1/flash-sale/buy`, trả `202 Accepted`)
  - `InventoryController.cs`: thao tác tồn kho (`/api/v1/inventory/...`)
  - `HealthController.cs`: health check (`/api/v1/health`)
  - `MetricsController.cs`: xuất metrics Prometheus (`/api/v1/metrics`)
  - `OutboxController.cs`: purge Outbox (`/api/v1/outbox/purge`, bảo vệ `[Authorize]`)
- `Middleware/`
  - `ExceptionHandlerMiddleware.cs`: chuẩn hóa lỗi trả về
  - `RateLimitMiddleware.cs`: kiểm tra RateLimit (FR-01, NFR-04), chạy sau Authentication
- `Program.cs`: cấu hình Serilog, DI, JWT Bearer, Swagger, HTTPS redirect (non-dev), middleware pipeline, EF migrate + seed admin

## Lưu ý vận hành
- Endpoint nghiệp vụ yêu cầu Bearer token (trừ health/root tùy cấu hình).
- `RateLimitMiddleware` lấy `ClientId` từ claim người dùng đã xác thực.
- Cấu hình qua environment variables (`POSTGRESQL_PRIMARY_CONNECTION_STRING`, `REDIS_CONNECTION_STRING`, `RABBITMQ_CONNECTION_STRING`, `JWT_*`, ...).

## Chạy local
```bash
dotnet run --project src/FlashSale.Api/FlashSale.Api.csproj
```
