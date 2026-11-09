# FlashSale.Api.Tests

Unit test cho layer API.

## Cấu trúc

| File | Mô tả |
|------|-------|
| `Unit/RateLimitMiddlewareTests.cs` | Test RateLimitMiddleware |
| `Unit/InventoryControllerTests.cs` | Test InventoryController |
| `Unit/HttpsRedirectionTests.cs` | Test bắt buộc HTTPS |
| `Unit/ApiSmokeTest.cs` | Test khói API |

## Cách chạy

```bash
dotnet test tests/FlashSale.Api.Tests -c Release
```
