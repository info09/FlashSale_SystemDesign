# Commands — Inventory (Application/Features)

Chứa command cho nghiệp vụ tồn kho.

## File
- `AddInventoryCommand.cs`: yêu cầu thêm tồn kho.
- `DeductInventoryCommand.cs`: yêu cầu trừ tồn kho (Atomic Inventory Offloading).

## Quy tắc
- Command chỉ mang dữ liệu; xử lý thực hiện ở handler/service.
- Thuật ngữ theo `docs/domain-glossary.md` (`Inventory`, không dùng `Stock`).
