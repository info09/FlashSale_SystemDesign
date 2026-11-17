# Services (FlashSale.Worker)

Chứa các hosted/background services của Worker.

## Services
- `Worker.cs`: vòng lặp consume `order_pipeline_queue`, xử lý message, áp dụng retry exponential backoff và chuyển DLQ sau số lần thất bại quy định (FR-04, FR-05).
- `OutboxPublisherWorkerService.cs`: quét và publish Outbox pending định kỳ (mặc định ~30s).
- `OutboxPurgeWorkerService.cs`: purge/archive Outbox đã xử lý theo chu kỳ/cấu hình (NFR-06).
- `OrderNotificationWorkerService.cs`: consume queue notification downstream.
- `MetricWorkerService.cs`: thu thập metric định kỳ từ hệ thống.
- `MetricEndpointService.cs`: host endpoint HTTP `/api/v1/metrics` để Prometheus scrape.

## Quy tắc
- Mỗi service có trách nhiệm rõ, chạy độc lập theo chu kỳ/event loop phù hợp.
- Tôn trọng cấu hình timeout/retry/purge; không hardcode secret.
- Đảm bảo xử lý lỗi có log và không làm sập vòng lặp worker ngoài ý muốn.
