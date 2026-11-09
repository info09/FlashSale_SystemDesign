# Folder: CommandHandlers (Auth)

Chứa các Command Handler của tính năng Xác thực (Auth) trong Application layer.

| File | Chức năng |
|------|-----------|
| `LoginCommandHandler.cs` | Xử lý Command đăng nhập (login). |
| `RegisterCommandHandler.cs` | Xử lý Command đăng ký (register). |

## Quy tắc

- Handler KHÔNG chứa business logic phức tạp, chỉ orchestrate Domain Service và Repository.
- Tên Handler phải hậu tố `CommandHandler` theo Convention 1.2.
