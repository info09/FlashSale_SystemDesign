# Middleware (FlashSale.Api)

Chứa middleware cross-cutting cho API.

## Middleware hiện có
- `ExceptionHandlerMiddleware.cs`: bắt exception, ghi log và trả response lỗi chuẩn.
- `RateLimitMiddleware.cs`: kiểm tra giới hạn tần số request theo client (Redis Token Bucket), trả `429` khi vượt ngưỡng.

## Quy tắc
- Không nhúng business logic nghiệp vụ.
- `RateLimitMiddleware` phải đặt sau `UseAuthentication()`/`UseAuthorization()` để đọc được identity/claims khi cần.
- Dùng logging có cấu trúc (Serilog), không `Console.WriteLine`.
