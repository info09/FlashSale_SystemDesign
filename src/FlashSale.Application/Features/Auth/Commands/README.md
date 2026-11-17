# Commands — Auth (Application/Features)

Chứa các command cho luồng xác thực/đăng ký người dùng.

## File
- `LoginCommand.cs`: dữ liệu đầu vào cho đăng nhập.
- `RegisterCommand.cs`: dữ liệu đầu vào cho đăng ký tài khoản.

## Quy tắc
- Command là DTO bất biến/record mang dữ liệu request vào tầng xử lý.
- Tên hậu tố `Command`, thuật ngữ theo `docs/domain-glossary.md` và quyết định auth hiện tại (Identity + JWT).
