# FlashSale.Worker.Tests

Unit tests cho `FlashSale.Worker`.

## Phạm vi tiêu biểu
- `WorkerTests`: xử lý message, retry, DLQ, trường hợp thành công/thất bại.
- `OrderCreatedConsumerServiceTests`, `OrderNotificationConsumerServiceTests`
- `OutboxPublisherWorkerServiceTests`, `OutboxPurgeWorkerServiceTests`, `OrderNotificationWorkerServiceTests`
- `ExponentialBackoffPolicyTests`, `WorkerSmokeTest`

## Chạy test
```bash
dotnet test tests/FlashSale.Worker.Tests -c Release
```

## Quy tắc
- Mock RabbitMQ/DB/service dependencies để kiểm thử logic worker/consumer cô lập.
- Bao phủ nhánh retry, idempotency và chuyển DLQ khi có.
