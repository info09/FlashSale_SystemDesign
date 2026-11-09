# FlashSale Engine — Backend API Core & Pipeline

Hệ thống Flash Sale distribuído, sự kiện-driven, chống over-selling, không mất đơn hàng, p99 latency dưới 20ms trên luồng nhận đơn.

## Tech stack

- .NET 8, ASP.NET Core
- Clean Architecture + CQRS
- PostgreSQL Primary (ghi Order + Outbox)
- Redis (RateLimit TokenBucket, Inventory atomic qua Lua Script)
- RabbitMQ (OrderPipeline, DeadLetterQueue, OrderNotification)
- Docker + Docker Compose
- xUnit, FluentAssertions, Moq, Testcontainers
- k6 (load test 8.000 VUs)

## Cấu trúc solution

### src/FlashSale.Domain

Layer Domain. Không tham chiếu layer nào khác.

- `Entities/`: Order, Outbox, Order, v.v.
- `Exceptions/`: DeadLetterQueueException, InsufficientInventoryException, RateLimitExceededException.
- `Interfaces/`: IOrderRepository, IOutboxRepository, IReconciliationRepository, IUnitOfWork.
- `RabbitMqConstants.cs`: tên Exchange/Queue/Routing Key chuẩn.

### src/FlashSale.Application

Layer Application. Chỉ tham chiếu Domain.

- `Interfaces/`: IOrderService, IInventoryService, IOutboxService, IOutboxPublisher, IReconciliationService, IOutboxPurgeService, IRabbitMqPublisherService.
- `Services/`: OrderService, OutboxService, OutboxPublisher, OutboxPurgeService, ReconciliationService, ExponentialBackoffPolicy, OutboxPurgeOptions.
- `Features/`: Auth, Inventory, RateLimit (Commands, CommandHandlers, Validators, Events).
- `DependencyInjection.cs`: đăng ký dịch vụ Application.

### src/FlashSale.Infrastructure

Layer Infrastructure. Tham chiếu Domain + Application.

- `Persistence/`: PostgreSQLPrimaryContext, OrderRepository, OutboxRepository, ReconciliationRepository, UnitOfWork, Migrations.
- `ExternalServices/RabbitMQ/`: RabbitMqPublisherService.
- `ExternalServices/Redis/`: RedisRateLimitService, RedisInventoryService, RedisScriptExecutor, Scripts.
- `DependencyInjection.cs`: đăng ký dịch vụ Infrastructure.

### src/FlashSale.Api

Layer API. Tham chiếu Application + Infrastructure.

- `Controllers/`: FlashSaleController, InventoryController, AuthController, OutboxController.
- `Middleware/`: RateLimitMiddleware, ExceptionHandlerMiddleware.
- `Program.cs`: cấu hình host, authentication, middleware, migration seed.
- `appsettings.json`, `appsettings.Development.json`.

### src/FlashSale.Worker

Async Worker (.NET Background Service). Tham chiếu Application + Infrastructure.

- `Consumers/`: OrderCreatedConsumerService, OrderNotificationConsumerService.
- `Services/`: Worker, OutboxPurgeWorkerService, OutboxPublisherWorkerService.
- `Program.cs`: cấu hình host, RabbitMQ connection/channel, DI, BackgroundService.

### tests/

- `FlashSale.Domain.Tests/`: unit test Domain.
- `FlashSale.Application.Tests/`: unit test Application (OrderService, ReconciliationService, OutboxPurgeService).
- `FlashSale.Infrastructure.Tests/`: integration test Infrastructure (Testcontainers PostgreSQL, Redis, RabbitMQ).
- `FlashSale.Api.Tests/`: unit test API.
- `FlashSale.Worker.Tests/`: unit test Worker (Worker, OutboxPurgeWorkerService, OutboxPublisherWorkerService, OrderNotificationConsumerService).
- `Integration.Tests/`: end-to-end test toàn bộ luồng (Testcontainers).

## Quy tắc đặt tên

Mọi tên class, bảng, cột, queue, exchange, routing key phải trùng thuật ngữ chuẩn trong `docs/domain-glossary.md`. Không được dùng từ đồng nghĩa (ví dụ: không gọi `Stock`, `Dlq`, `GatewayController`).

## Cách chạy

1. Copy `.env.example` thành `.env`, điền giá trị thật.
2. `docker-compose up -d` để khởi tạo toàn bộ dịch vụ.
3. `dotnet build FlashSale.sln -c Release` để build.
4. `dotnet test FlashSale.sln -c Release` để chạy toàn bộ test.

## Quy tắc commit

- Không tự ý commit.
- Commit message theo convention: `{type}: {ma-FR} {mo-ta}` (ví dụ: `feat: FR-04 implement OrderService`).
- Loại commit: feat, fix, docs, style, refactor, test, chore, ci.

## Gợi ý cải tiến

- Thêm `OutboxPublisherWorkerService` đã chạy mỗi 30 giây. Có thể giảm interval nếu cần publish gần real-time.
- Thêm reconciliation chạy định kỳ (hiện có service, cần scheduler hoặc gọi thủ công qua API nếu có).
- Thêm HealthCheck endpoint và Prometheus/Grafana (Phase 5, chưa triển khai).
