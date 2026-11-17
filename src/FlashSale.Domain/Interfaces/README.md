# Interfaces (FlashSale.Domain)

Chứa abstraction cho truy cập dữ liệu và thành phần cần thiết của domain.

## Interface tiêu biểu
- `IOrderRepository.cs`, `IOutboxRepository.cs`: thao tác Order/Outbox.
- `IInventoryRepository.cs`, `IRateLimitRepository.cs`: thao tác trạng thái Inventory/RateLimit.
- `IUserRepository.cs`, `IUserService.cs`, `IJwtTokenService.cs`: phục vụ Identity/JWT.
- `IUnitOfWork.cs`: quản lý transaction nhất quán.
- `IReconciliationRepository.cs`: hỗ trợ đối chiếu dữ liệu.
- `IDatabaseConnectionChecker.cs`: kiểm tra kết nối cơ sở dữ liệu.

## Quy tắc
- Chỉ định nghĩa contract; không phụ thuộc EF Core, Redis, RabbitMQ hay framework cụ thể.
- Tên/semantics bám `docs/domain-glossary.md`.
