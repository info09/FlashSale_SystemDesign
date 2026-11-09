# Folder: Controllers (Api)

Chứa các Controller của API layer. Tham chiếu `FlashSale.Application` + `FlashSale.Infrastructure`.

| File | Chức năng |
|------|-----------|
| `FlashSaleController.cs` | Endpoint mua hàng (`/api/v1/flash-sale/buy`), trả HTTP 202 Accepted (FR-03). |
| `InventoryController.cs` | Endpoint kiểm tra/trừ kho (`/api/v1/inventory/...`, FR-02). |
| `AuthController.cs` | Endpoint đăng ký/đăng nhập (`/api/v1/auth/register`, `/api/v1/auth/login`). |
| `OutboxController.cs` | Endpoint purge Outbox (`/api/v1/outbox/purge`, bảo vệ `[Authorize]`, NFR-06). |

## Quy tắc

- Controller KHÔNG chứa business logic, chỉ nhận request, gọi Service/MediatR, trả response.
- Controller KHÔNG gọi trực tiếp DbContext, Redis, RabbitMQ. Gọi qua Application Service hoặc MediatR.
- Mỗi action phải có `[ProducesResponseType]` cho status code chính.
- Route dùng kebab-case (Convention 1.6).
