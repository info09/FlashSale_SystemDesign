# Folder: CommandHandlers (Inventory)

Chứa các Command Handler của tính năng Kho hàng (Inventory) trong Application layer.

| File | Chức năng |
|------|-----------|
| `AddInventoryCommandHandler.cs` | Xử lý Command thêm tồn kho. |
| `DeductInventoryCommandHandler.cs` | Xử lý Command trừ tồn kho atomic (FR-02). |

## Quy tắc

- Handler KHÔNG chứa business logic phức tạp, chỉ orchestrate Domain Service và Repository.
- Tên Handler phải hậu tố `CommandHandler` theo Convention 1.2.
