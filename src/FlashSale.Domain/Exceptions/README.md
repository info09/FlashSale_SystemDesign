# Exceptions (FlashSale.Domain)

Chứa domain exceptions biểu diễn lỗi nghiệp vụ/kỹ thuật có ngữ nghĩa trong domain.

## Exception tiêu biểu
- `InsufficientInventoryException.cs`: không đủ tồn kho để trừ (chống over-selling).
- `RateLimitExceededException.cs`: vượt ngưỡng giới hạn tần số request.
- `DeadLetterQueueException.cs`: lỗi liên quan xử lý/chuyển message vào DLQ.

## Quy tắc
- Exception mang ý nghĩa domain rõ ràng; mapping HTTP/status code thực hiện ở tầng API/middleware.
- Tên theo thuật ngữ chuẩn trong `docs/domain-glossary.md`.
