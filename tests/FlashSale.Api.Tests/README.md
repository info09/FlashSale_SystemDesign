# FlashSale.Api.Tests

Unit tests cho `FlashSale.Api`.

## Phạm vi tiêu biểu
- `RateLimitMiddlewareTests`: hành vi chặn/cho phép request theo rate limit.
- `InventoryControllerTests`: hành vi endpoint/controller tồn kho.
- `HttpsRedirectionTests`: yêu cầu/redirect HTTPS theo cấu hình.
- `ApiSmokeTest`: khởi tạo/cấu hình cơ bản API.

## Chạy test
```bash
dotnet test tests/FlashSale.Api.Tests -c Release
```

## Quy tắc
- Tập trung vào controller/middleware/pipeline HTTP, mock dependency bên dưới.
- Kiểm tra status code, contract response và điều kiện auth/rate limit khi liên quan.
