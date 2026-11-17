# CommandHandlers — Inventory (Application/Features)

Chứa handler xử lý command tồn kho.

## File
- `AddInventoryCommandHandler.cs`: xử lý thêm tồn kho.
- `DeductInventoryCommandHandler.cs`: xử lý trừ tồn kho atomic.

## Quy tắc
- Handler gọi service/interface phù hợp, không truy cập trực tiếp Redis/DB.
- Tên hậu tố `CommandHandler`.
