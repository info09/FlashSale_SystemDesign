# Folder: Exceptions (Domain)

Chứa các domain exception của hệ thống Flash Sale.

| File | Chức năng |
|------|-----------|
| `DeadLetterQueueException.cs` | Ngoại lệ khi chuyển message vào DeadLetterQueue thất bại (FR-05). |
| `InsufficientInventoryException.cs` | Ngoại lệ khi hết kho (AtomicInventoryOffloading, FR-02). |
| `RateLimitExceededException.cs` | Ngoại lệ khi vượt ngưỡng rate limit (FR-01, NFR-04). |

## Quy tắc

- Domain exception KHÔNG chứa mã HTTP status code (chỉ message lỗi).
- Tên exception phải trùng thuật ngữ chuẩn trong `docs/domain-glossary.md`.
