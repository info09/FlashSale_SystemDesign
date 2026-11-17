# Interfaces (FlashSale.Application)

Chứa các interface của Application layer cho service/use-case và điểm tích hợp hạ tầng.

## Interface tiêu biểu
- `IOrderService.cs`: tạo/xử lý nghiệp vụ Order.
- `IInventoryService.cs`: nghiệp vụ tồn kho (trừ/thêm/kiểm tra tùy triển khai).
- `IRateLimitService.cs`: kiểm tra rate limit.
- `IOutboxService.cs`: thao tác nghiệp vụ với Outbox.
- `IOutboxPublisher.cs`: publish sự kiện Outbox ra messaging.
- `IOutboxPurgeService.cs`: purge/archive Outbox đã xử lý.
- `IReconciliationService.cs`: đối chiếu dữ liệu Order/Outbox.
- `IRabbitMqPublisherService.cs`: publish message sang RabbitMQ.
- `IMetricService.cs`, `IMetricCollector.cs`: thu thập/xuất metric (nếu có trong module).

## Quy tắc
- Interface mô tả hành vi, không chứa implementation hay phụ thuộc hạ tầng cụ thể.
- Tên và ngữ nghĩa bám `docs/domain-glossary.md` (Order, Outbox, Inventory, RateLimit, ...).
