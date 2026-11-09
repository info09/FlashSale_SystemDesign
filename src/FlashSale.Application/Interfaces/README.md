# Folder: Interfaces (Application)

Chứa các giao diện dịch vụ của Application layer.

| File | Chức năng |
|------|-----------|
| `IOrderService.cs` | Giao diện dịch vụ Order (tạo đơn hàng). |
| `IInventoryService.cs` | Giao diện dịch vụ Inventory (trừ kho atomic). |
| `IOutboxService.cs` | Giao diện dịch vụ Outbox (quản lý ghi sự kiện vào bảng Outbox). |
| `IOutboxPublisher.cs` | Giao diện dịch vụ xuất phat sự kiện Outbox ra RabbitMQ. |
| `IOutboxPurgeService.cs` | Giao diện dịch vụ purge/archive bảng Outbox (NFR-06). |
| `IReconciliationService.cs` | Giao diện dịch vụ Reconciliation (đối chiếu Order/Outbox, NFR-06). |
| `IRabbitMqPublisherService.cs` | Giao diện dịch vụ publish message vào RabbitMQ. |
| `IRateLimitService.cs` | Giao diện dịch vụ RateLimit. |

## Quy tắc

- Không chứa `DbContext` trực tiếp trong Application, phải qua Repository Interface.
