# FlashSale.Worker.Tests

Unit test cho layer Worker.

## Cấu trúc

| File | Mô tả |
|------|-------|
| `Unit/WorkerTests.cs` | Test Worker.HandleDeliveryAsync (retry, DLQ, thành công) |
| `Unit/OrderNotificationConsumerServiceTests.cs` | Test consumer downstream |
| `Unit/OrderNotificationWorkerServiceTests.cs` | Test BackgroundService consume notification |
| `Unit/OutboxPurgeWorkerServiceTests.cs` | Test BackgroundService purge Outbox |
| `Unit/OutboxPublisherWorkerServiceTests.cs` | Test BackgroundService publish Outbox |
| `Unit/OrderCreatedConsumerServiceTests.cs` | Test OrderCreatedConsumerService |
| `Unit/ExponentialBackoffPolicyTests.cs` | Test chính sách backoff |
| `Unit/WorkerSmokeTest.cs` | Test khói Worker |

## Cách chạy

```bash
dotnet test tests/FlashSale.Worker.Tests -c Release
```
