# FlashSale Engine — Backend API Core & Pipeline

Hệ thống Flash Sale phân tán, event-driven, chống over-selling, không mất đơn đã nhận `202 Accepted`, tối ưu fast-path nhận đơn.

## Tech stack
- .NET 8, ASP.NET Core, .NET Background Service
- Clean Architecture + CQRS-style features
- PostgreSQL Primary (Order, Outbox, Identity) + EF Core migrations
- Redis (RateLimit Token Bucket, Inventory atomic) + Lua Script
- RabbitMQ (OrderPipeline, DLQ, OrderNotification)
- ASP.NET Core Identity + JWT Bearer
- Serilog, Swagger/OpenAPI
- Prometheus/Grafana (metrics/health observability)
- Docker + Docker Compose
- xUnit, FluentAssertions, Moq, Testcontainers, k6

## Cấu trúc solution

### `src/FlashSale.Domain`
Layer lõi, không phụ thuộc layer khác.
- `Entities/`: `Order`, `Outbox`, `Inventory`, `RateLimit`, `User`, `Role`, `HealthResult`, `Metric`, `ReconciliationResult`, ...
- `Interfaces/`: repository/service abstractions (`IOrderRepository`, `IOutboxRepository`, `IInventoryRepository`, `IRateLimitRepository`, `IUserRepository`, `IUnitOfWork`, `IReconciliationRepository`, `IDatabaseConnectionChecker`, ...)
- `Exceptions/`: `InsufficientInventoryException`, `RateLimitExceededException`, `DeadLetterQueueException`
- `RabbitMqConstants.cs`: tên Exchange/Queue/Routing Key chuẩn

### `src/FlashSale.Application`
Orchestration/use-case, chỉ phụ thuộc Domain.
- `Interfaces/`: `IOrderService`, `IInventoryService`, `IOutboxService`, `IOutboxPublisher`, `IOutboxPurgeService`, `IReconciliationService`, `IRabbitMqPublisherService`, `IRateLimitService`, `IMetricService`, `IMetricCollector`, ...
- `Services/`: `OrderService`, `OutboxService`, `OutboxPublisher`, `OutboxPurgeService`, `ReconciliationService`, `InventoryService`, `RateLimitService`, `MetricService`, `ExponentialBackoffPolicy`, `OutboxPurgeOptions`, ...
- `Features/`: `Auth`, `Inventory`, `RateLimit` (Commands/CommandHandlers/Validators/Events)
- `DependencyInjection.cs`

### `src/FlashSale.Infrastructure`
Triển khai hạ tầng, phụ thuộc Domain + Application.
- `Persistence/`: `PostgreSQLPrimaryContext`, repositories, `UnitOfWork`, `AuthService`, `JwtTokenService`, `HealthCheckService`, `MetricCollector`, `AdminSeeder`, `Migrations/`
- `ExternalServices/Redis/`: `RedisRateLimitService`, `RedisInventoryService`, `RedisScriptExecutor`, `Scripts/*.lua`
- `ExternalServices/RabbitMQ/`: `RabbitMqPublisherService`
- `DependencyInjection.cs`: đăng ký PostgreSQL/Identity, Redis, RabbitMQ, repositories, services

### `src/FlashSale.Api`
HTTP API, phụ thuộc Application + Infrastructure.
- `Controllers/`: `AuthController`, `FlashSaleController`, `InventoryController`, `HealthController`, `MetricsController`, `OutboxController`
- `Middleware/`: `RateLimitMiddleware`, `ExceptionHandlerMiddleware`
- `Program.cs`: Serilog, DI, JWT Bearer, Swagger, HTTPS redirect (non-dev), auth/rate-limit pipeline, EF migrate + seed

### `src/FlashSale.Worker`
Async worker, phụ thuộc Application + Infrastructure.
- `Consumers/`: `OrderCreatedConsumerService`, `OrderNotificationConsumerService`
- `Services/`: `Worker`, `OutboxPublisherWorkerService`, `OutboxPurgeWorkerService`, `OrderNotificationWorkerService`, `MetricWorkerService`, `MetricEndpointService`
- `Program.cs`: DI, RabbitMQ connection/channel, hosted services, metrics endpoint

### `tests/`
- `FlashSale.Domain.Tests`, `FlashSale.Application.Tests`, `FlashSale.Infrastructure.Tests`, `FlashSale.Api.Tests`, `FlashSale.Worker.Tests`
- `Integration.Tests`: luồng end-to-end với Testcontainers (PostgreSQL/Redis/RabbitMQ)

## Luồng chính
- **Auth:** `POST /api/v1/auth/register`, `POST /api/v1/auth/login` (JWT Bearer).
- **Rate limit (FR-01):** `RateLimitMiddleware` kiểm tra Redis Token Bucket trước xử lý tiếp.
- **Inventory (FR-02):** trừ kho atomic trên Redis qua Lua script, hết kho không tạo đơn.
- **Mua hàng (FR-03):** `POST /api/v1/flash-sale/buy` trừ kho + publish message vào `order_pipeline_queue` + trả `202 Accepted`.
- **Xử lý async/Outbox (FR-04, FR-05):** Worker consume message, ghi `Order` + `Outbox` trong cùng transaction PostgreSQL, retry exponential backoff, DLQ sau 3 lần; có publish outbox pending và notification downstream.
- **Observability (FR-06):** `GET /api/v1/health`, `GET /api/v1/metrics`; Worker expose metrics cho Prometheus scrape.

## Chạy dự án
1. Tạo `.env` từ `.env.example` và điền giá trị.
2. Khởi chạy hạ tầng/app: `docker-compose up -d`
3. Build: `dotnet build FlashSale.sln -c Release`
4. Test: `dotnet test FlashSale.sln -c Release`

API mặc định: `http://localhost:8080` (theo compose). Worker metrics: `http://localhost:8081/api/v1/metrics`.

## Quy ước
- Tên class/bảng/cột/queue/exchange/routing key phải theo `docs/domain-glossary.md`.
- Không commit tự ý; commit theo `{type}: {ma-FR} {mo-ta}` khi được yêu cầu.
- Ưu tiên đọc `docs/status.md`, `docs/architecture.md`, `docs/decisions.md` để nắm tiến độ và quyết định hiện tại.
