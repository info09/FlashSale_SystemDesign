# CommandHandlers — Auth (Application/Features)

Chứa handler xử lý command xác thực/đăng ký.

## File
- `LoginCommandHandler.cs`: xử lý đăng nhập, trả kết quả/token theo luồng auth.
- `RegisterCommandHandler.cs`: xử lý đăng ký người dùng.

## Quy tắc
- Handler orchestrate service/repository qua interface, không nhúng chi tiết hạ tầng.
- Tên hậu tố `CommandHandler`.
