# Validators — Inventory (Application/Features)

Chứa FluentValidation validators cho command Inventory.

## File
- `AddInventoryCommandValidator.cs`: validate request thêm tồn kho.
- `DeductInventoryCommandValidator.cs`: validate request trừ tồn kho.

## Quy tắc
- Đảm bảo `ProductId`, số lượng hợp lệ trước khi xử lý nghiệp vụ.
- Tên hậu tố `Validator`.
