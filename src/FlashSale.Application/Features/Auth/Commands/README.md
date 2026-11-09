# Folder: Commands (Auth)

Chứa các Command của tính năng Xác thực (Auth) trong Application layer.

| File | Chức năng |
|------|-----------|
| `LoginCommand.cs` | Command đăng nhập người dùng. |
| `RegisterCommand.cs` | Command đăng ký người dùng mới. |

## Quy tắc

- Tên Command phải hậu tố `Command` theo Convention 1.1.
- Dùng MediatR cho CQRS dispatching.
