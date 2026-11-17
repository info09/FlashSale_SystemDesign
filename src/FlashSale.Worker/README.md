# FlashSale.Worker

Background worker xử lý bất đồng bộ: consume OrderPipeline, đảm bảo ghi Order/Outbox, retry/DLQ, publish outbox pending, notification downstream, và thu thập/expose metrics.

## Thành phần
- `Consumers/`
  - `OrderCreatedConsumerService.cs`: xử lý message tạo đơn từ `order_pipeline_queue`.
  - `OrderNotificationConsumerService.cs`: xử lý message notification downstream.
- `Services/`
  - `Worker.cs`: hosted service consume pipeline chính, retry + DLQ.
  - `OutboxPublisherWorkerService.cs`: publish Outbox pending định kỳ.
  - `OutboxPurgeWorkerService.cs`: purge/archive Outbox định kỳ.
  - `OrderNotificationWorkerService.cs`: consume notification queue.
  - `MetricWorkerService.cs`: thu thập metric định kỳ.
  - `MetricEndpointService.cs`: expose endpoint `/api/v1/metrics` (cho Prometheus scrape).
- `Program.cs`: cấu hình DI, RabbitMQ connection/channel, đăng ký hosted services.

## Chạy local
```bash
dotnet run --project src/FlashSale.Worker/FlashSale.Worker.csproj
```

## Lưu ý
- Cần cấu hình `POSTGRESQL_PRIMARY_CONNECTION_STRING`, `RABBITMQ_CONNECTION_STRING`, ... qua environment.
- Worker metrics mặc định phục vụ qua endpoint riêng (xem `MetricEndpointService`/compose, thường map cổng `8081`).

## Test
```bash
dotnet test tests/FlashSale.Worker.Tests -c Release
```
