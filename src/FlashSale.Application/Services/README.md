# Folder: Services (Application)

Chứa các dịch vụ nghiệp vụ của Application layer.

| File | Chức năng |
|------|-----------|
| `OrderService.cs` | Dịch vụ tạo đơn hàng (Order) sau khi trừ kho thành công (FR-04). |
| `InventoryService.cs` | Dịch vụ trừ kho atomic trên Redis qua Lua Script (FR-02). |
| `OutboxService.cs` | Dịch vụ quản lý ghi sự kiện vào bảng Outbox. |
| `OutboxPublisher.cs` | Dịch vụ xuất phat ghi Outbox Pending ra RabbitMQ Queue. |
| `OutboxPurgeService.cs` | Dịch vụ purge/archive bảng Outbox cũ hơn PurgeDays (NFR-06). |
| `OutboxPurgeOptions.cs` | Cấu hình thời gian purge/archive bảng Outbox (PurgeDays). |
| `ReconciliationService.cs` | Dịch vụ đối chiếu Order và Outbox (NFR-06). |
| `ExponentialBackoffPolicy.cs` | Chính sách Retry với Exponential Backoff (FR-05). |
| `RateLimitService.cs` | Dịch vụ kiểm tra rate limit qua Redis Lua Script (FR-01). |

## Quy tắc

- Service chỉ orchestrate Domain Service và Repository, không chứa business logic phức tạp.
- Không dùng `DbContext` trực tiếp trong Application.
