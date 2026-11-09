# Folder: CommandHandlers (RateLimit)

Chứa các Command Handler của tính năng Giới hạn tần số (RateLimit) trong Application layer.

| File | Chức năng |
|------|-----------|
| `RateLimitCommandHandler.cs` | Xử lý Command kiểm tra rate limit. |

## Quy tắc

- Handler KHÔNG chứa business logic phức tạp, chỉ orchestrate Domain Service và Repository.
- Tên Handler phải hậu tố `CommandHandler` theo Convention 1.2.
