# CommandHandlers — RateLimit (Application/Features)

Chứa handler xử lý command RateLimit.

## File
- `RateLimitCommandHandler.cs`: gọi service kiểm tra rate limit và trả kết quả cho pipeline/middleware.

## Quy tắc
- Không truy cập Redis trực tiếp; dùng abstraction/service.
- Tên hậu tố `CommandHandler`.
