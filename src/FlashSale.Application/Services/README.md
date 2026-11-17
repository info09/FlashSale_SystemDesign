# Services (FlashSale.Application)

Chứa các service triển khai nghiệp vụ/orchestration của Application layer.

## Service chính
- `OrderService.cs`: tạo đơn và ghi Outbox trong cùng transaction PostgreSQL (Transactional Outbox Pattern, FR-04/NFR-06).
- `InventoryService.cs`: xử lý nghiệp vụ tồn kho, phối hợp repository/Redis implementation qua interface.
- `RateLimitService.cs`: kiểm tra rate limit theo chính sách cấu hình.
- `OutboxService.cs`: thêm/cập nhật bản ghi Outbox phục vụ outbox pattern.
- `OutboxPublisher.cs`: đọc Outbox pending và publish ra queue/notification.
- `OutboxPurgeService.cs`, `OutboxPurgeOptions.cs`: dọn Outbox đã xử lý theo thời gian giữ.
- `ReconciliationService.cs`: đối chiếu tính nhất quán Order/Outbox.
- `ExponentialBackoffPolicy.cs`: tính toán delay retry cho xử lý message.
- `MetricService.cs`: tổng hợp/định dạng dữ liệu metric.

## Quy tắc
- Service orchestrate dependency qua interface; không truy cập trực tiếp hạ tầng cụ thể.
- Giữ transaction boundary rõ ràng khi liên quan `Order` + `Outbox`.
- Không hardcode secret/cấu hình nhạy cảm.
