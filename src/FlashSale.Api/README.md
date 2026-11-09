# FlashSale.Api

Layer API của hệ thống Flash Sale. Chịu trách nhiệm nhận request từ client, xác thực JWT, kiểm tra rate limit, xử lý mua hàng, purge Outbox.

## Thuộc thuộc vào

- `FlashSale.Application`
- `FlashSale.Infrastructure`

## Cấu trúc

| Thư mục | Mô tả |
|---------|-------|
| `Controllers/` | FlashSaleController, InventoryController, AuthController, OutboxController |
| `Middleware/` | RateLimitMiddleware, ExceptionHandlerMiddleware |
| `Program.cs` | Cấu hình host, authentication, middleware, migration seed |

## Endpoint chính

| Phương thức | Đường dẫn | Mô tả |
|-------------|-----------|-------|
| POST | `/api/v1/auth/register` | Đăng ký người dùng |
| POST | `/api/v1/auth/login` | Đăng nhập, nhận JWT |
| POST | `/api/v1/flash-sale/buy` | Mua hàng (trả 202 Accepted) |
| GET | `/api/v1/inventory/{productId}` | Kiểm tra tồn kho |
| POST | `/api/v1/outbox/purge` | Purge Outbox (bảo vệ `[Authorize]`) |
| GET | `/health` | Health check |

## Cách chạy

```bash
dotnet run --project src/FlashSale.Api/FlashSale.Api.csproj
```

Cần cấu hình biến môi trường: `POSTGRESQL_PRIMARY_CONNECTION_STRING`, `REDIS_CONNECTION_STRING`, `RABBITMQ_CONNECTION_STRING`, `JWT_SECRET`, v.v. (xem `.env.example`).

## Lưu ý

- Tất cả endpoint bắt buộc HTTPS và Bearer Token (trừ `/health`, `/`).
- RateLimitMiddleware chạy sau Authentication để lấy `ClientId` từ JWT.
