# Folder: Middleware (Api)

Chứa các Middleware của API layer.

| File | Chức năng |
|------|-----------|
| `RateLimitMiddleware.cs` | Kiểm tra RateLimit trước khi xử lý tiếp, trả HTTP 429 nếu vượt ngưỡng (FR-01, NFR-04). |
| `ExceptionHandlerMiddleware.cs` | Bắt toan bộ exception, trả JSON lỗi chuẩn. |

## Quy tắc

- Middleware chỉ xử lý 관심 chung (cross-cutting concern), không chứa business logic.
- RateLimitMiddleware chạy sau Authentication để lấy `ClientId` từ JWT.
