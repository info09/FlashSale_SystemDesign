# FlashSale.Application

Layer Application của hệ thống Flash Sale. Chứa logic nghiệp vụ, CQRS (Command, Validator), dịch vụ ứng dụng. Chỉ tham chiếu `FlashSale.Domain`.

## Cấu trúc

| Thư mục | Mô tả |
|---------|-------|
| `Interfaces/` | Giao diện dịch vụ (IOrderService, IInventoryService, IOutboxService, IOutboxPublisher, IReconciliationService, IOutboxPurgeService, IRabbitMqPublisherService) |
| `Services/` | OrderService, OutboxService, OutboxPublisher, OutboxPurgeService, ReconciliationService, ExponentialBackoffPolicy, OutboxPurgeOptions |
| `Features/Auth/` | LoginCommand, RegisterCommand, handlers, validators |
| `Features/Inventory/` | DeductInventoryCommand, OrderCreatedMessage |
| `Features/RateLimit/` | RateLimitCommand, validators |
| `DependencyInjection.cs` | Đăng ký dịch vụ Application |

## Quy tắc

- Không chứa `DbContext`, Redis, RabbitMQ trực tiếp. Phải qua Repository Interface.
- Validator dùng FluentValidation, đặt trong `Features/{Tên}/Validators/`.
- Command/Query Handler chỉ orchestrate Domain Service và Repository.

## Cách chạy test

```bash
dotnet test tests/FlashSale.Application.Tests -c Release
```
