# Controllers (FlashSale.Api)

Chứa API controllers, chỉ nhận request/validate cơ bản ở tầng HTTP, gọi Application/Infrastructure service và trả response.

## Controllers hiện có
- `AuthController.cs`: đăng ký, đăng nhập, cấp JWT.
- `FlashSaleController.cs`: endpoint mua hàng flash sale, trừ kho và publish pipeline, trả `202 Accepted`.
- `InventoryController.cs`: kiểm tra/trừ/thêm tồn kho (tùy action triển khai).
- `HealthController.cs`: endpoint health check dịch vụ/phụ thuộc.
- `MetricsController.cs`: endpoint metrics dạng Prometheus text.
- `OutboxController.cs`: endpoint purge Outbox (có bảo vệ auth).

## Quy tắc
- Không chứa business logic phức tạp; orchestrate qua service/handler.
- Không truy cập trực tiếp DbContext/Redis/RabbitMQ.
- Khai báo `[ProducesResponseType]`, route/thuật ngữ đúng `docs/domain-glossary.md`.
- Endpoint cần bảo vệ phải dùng `[Authorize]` và JWT Bearer hợp lệ.
