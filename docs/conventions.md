# Coding Conventions — Distributed Flash-Sale Engine (.NET 8)

File này là chuẩn code dùng để tự review trước khi báo "done". Mỗi mục phải là điều kiện có thể trả lời đúng/sai (không phải khuyến nghị chung). Tech stack chốt: .NET 8 ASP.NET Core, Clean Architecture + CQRS, Redis Cluster + Lua Scripting, RabbitMQ + MassTransit, PostgreSQL Primary + Read Replica, Docker + Docker Compose.

Mọi tên class, entity, bảng dữ liệu, cột, queue, exchange, routing key, metric, endpoint PHẢI truy ngược được về đúng 1 thuật ngữ trong `docs/domain-glossary.md` (chỉ khác biệt ở quy tắc viết hoa/viết thường, không khác nghĩa).

## 1. Quy tắc chuyển đổi tên từ glossary sang code

Nguyên tắc: lấy cột "Thuật ngữ chuẩn" (tên tiếng Anh dùng trong code) trong `docs/domain-glossary.md`, áp dụng casing theo quy tắc dưới đây. Không được tự dịch lại hoặc dùng từ đồng nghĩa.

| Casing | Dùng cho | Quy tắc | Ví dụ từ glossary → Code |
|--------|----------|---------|--------------------------|
| PascalCase | Class, interface, method, event, enum, table, file | Viết hoa chữ cái đầu mỗi từ | `ApiGateway`, `AsyncWorker`, `Inventory`, `Order`, `Outbox`, `RateLimit`, `TokenBucket`, `LuaScript`, `DeadLetterQueue`, `ExponentialBackoff`, `PostgreSQLPrimary`, `ReadReplica`, `OrderPipeline`, `Http202Accepted`, `FastPath`, `FlashSale`, `Product`, `Prometheus`, `Grafana`, `P99Latency`, `ErrorRate`, `QueueDepth`, `DlqSize`, `InventoryRemaining`, `HealthCheck`, `Metric`, `Resilience`, `Retry`, `Message`, `Queue`, `RabbitMQ`, `DockerCompose`, `VirtualUser`, `Spike`, `Benchmarking`, `Uptime`, `SLA` |
| camelCase | Biến cục bộ, tham số, trường private, property JSON | Viết thường chữ cái đầu, hoa chữ cái đầu các từ sau | `apiGateway`, `asyncWorker`, `inventory`, `order`, `outbox`, `rateLimit`, `tokenBucket`, `luaScript`, `deadLetterQueue`, `exponentialBackoff`, `postgreSQLPrimary`, `readReplica`, `orderPipeline`, `http202Accepted`, `fastPath`, `flashSale`, `product`, `p99Latency`, `errorRate`, `queueDepth`, `dlqSize`, `inventoryRemaining`, `healthCheck`, `metric`, `resilience`, `retry`, `message`, `queue`, `rabbitMq`, `dockerCompose`, `virtualUser`, `spike`, `benchmarking`, `uptime`, `sla` |
| snake_case | Tên bảng DB (nếu chọn snake_case), tên cột DB, tên queue, exchange, routing key | Viết thường, cách bằng gạch dưới | `api_gateway`, `async_worker`, `inventory`, `order`, `outbox`, `rate_limit`, `token_bucket`, `lua_script`, `dead_letter_queue`, `exponential_backoff`, `postgresql_primary`, `read_replica`, `order_pipeline`, `http_202_accepted`, `fast_path`, `flash_sale`, `product`, `p99_latency`, `error_rate`, `queue_depth`, `dlq_size`, `inventory_remaining`, `health_check`, `metric`, `resilience`, `retry`, `message`, `queue`, `rabbit_mq`, `docker_compose`, `virtual_user`, `spike`, `benchmarking`, `uptime`, `sla` |
| kebab-case | Route URL, tên branch git | Viết thường, cách bằng gạch ngang | `/api/v1/flash-sale/buy`, `/api/v1/inventory`, `/api/v1/order-pipeline`, `/api/v1/health`, `/api/v1/metrics` |
| UPPER_SNAKE_CASE | Hằng số, enum | Viết hoa toàn bộ, cách bằng gạch dưới | `HTTP_STATUS_ACCEPTED`, `MESSAGE_STATUS_PUBLISHED`, `MESSAGE_STATUS_FAILED`, `MESSAGE_STATUS_MOVED_TO_DEAD_LETTER_QUEUE` |

Quy tắc chuyển đổi cụ thể:
- "Khách hàng săn sale" → `FlashSaleShopper` (class), `flashSaleShopper` (bien), `flash_sale_shopper` (DB/queue)
- "Kỹ sư vận hành hệ thống" → `DevOps` (class), `devOps` (bien)
- "202 Accepted" → `Http202Accepted` (class), `http202Accepted` (bien), `http_202_accepted` (DB/queue)
- "Dead Letter Queue" → `DeadLetterQueue` (class), `deadLetterQueue` (bien), `dead_letter_queue` (DB/queue)
- "Exponential Backoff" → `ExponentialBackoff` (class), `exponentialBackoff` (bien), `exponential_backoff` (DB/queue)
- "PostgreSQL Primary" → `PostgreSQLPrimary` (class), `postgreSQLPrimary` (bien), `postgresql_primary` (DB/queue)
- "Read Replica" → `ReadReplica` (class), `readReplica` (bien), `read_replica` (DB/queue)
- "Atomic Inventory Offloading" → `AtomicInventoryOffloading` (class), `atomicInventoryOffloading` (bien), `atomic_inventory_offloading` (DB/queue)
- "Event-Driven Order Pipeline" → `OrderPipeline` (class), `orderPipeline` (bien), `order_pipeline` (DB/queue)
- "Token Bucket" → `TokenBucket` (class), `tokenBucket` (bien), `token_bucket` (DB/queue)
- "Lua Script" → `LuaScript` (class), `luaScript` (bien), `lua_script` (DB/queue)
- "Transaction Outbox Pattern" → `Outbox` (class/bảng), `outbox` (bien), `outbox` (DB/queue)

### 1.1 Tên file
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Class file | PascalCase, trùng tên class | `InventoryService.cs`, `OrderCreatedConsumer.cs`, `RateLimitMiddleware.cs`, `DeductInventoryCommandHandler.cs` | `inventory_service.cs`, `Inventory_service.cs`, `InvSvc.cs` |
| Interface file | PascalCase, bắt đầu bằng I | `IInventoryService.cs`, `IOutboxRepository.cs`, `IRateLimitPolicy.cs`, `IOrderRepository.cs` | `i_inventory_service.cs`, `IstockRepository.cs` |
| Test file | Tên class test + `.Tests.cs` | `InventoryServiceTests.cs`, `OrderCreatedConsumerTests.cs`, `DeductInventoryCommandHandlerTests.cs` | `inventory_test.cs`, `TestInventory.cs` |
| Migration file | `{Timestamp}_{MucDich}.cs` | `20261104120000_CreateOrderTable.cs`, `20261104120001_AddOutboxTable.cs` | `create_order.cs`, `migration1.cs` |
| Lua script file | snake_case, `.lua` | `rate_limit_token_bucket.lua`, `atomic_inventory_deduct.lua` | `RateLimit.lua`, `deduct.lua` |
| Message contract | PascalCase, hậu tố `Message` | `OrderCreatedMessage.cs`, `InventoryDeductedMessage.cs` | `order_created.cs`, `OrderEvent.cs` |
| Command / Query (CQRS) | PascalCase, hậu tố `Command` / `Query` | `DeductInventoryCommand.cs`, `GetInventoryQuery.cs`, `CreateOrderCommand.cs` | `deduct_inventory.cs`, `OrderCommand.cs` |

### 1.2 Tên class / interface / struct / record (theo layer)
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Domain Entity | PascalCase, trùng glossary | `Order`, `Outbox`, `Product`, `Inventory`, `FlashSale` | `OrderItem`, `Purchase`, `Transaction`, `Sale`, `HoaDon`, `Stock` |
| Domain Event | PascalCase, hậu tố `Event` | `OrderCreatedEvent`, `InventoryDeductedEvent`, `RateLimitExceededEvent` | `OrderEvent`, `StockEvent`, `ThrottleEvent` |
| Domain Exception | PascalCase, hậu tố `Exception` | `InsufficientInventoryException`, `RateLimitExceededException` | `OutOfStockException`, `QuotaExceededException` |
| Domain Repository Interface | PascalCase, hậu tố `Repository`, đặt trong `Domain/` | `IOrderRepository`, `IOutboxRepository`, `IProductRepository` | `OrderDao`, `OutboxDao`, `EventStoreRepository` |
| Application Use Case / Handler | PascalCase, hậu tố `CommandHandler` / `QueryHandler` / `Service` | `DeductInventoryCommandHandler`, `CreateOrderCommandHandler`, `OrderService` | `DeductInventoryHandler`, `OrderManager`, `RateLimiter` |
| Application DTO / Request / Response | PascalCase, hậu tố rõ ràng | `CreateOrderRequest`, `OrderResponse`, `HealthCheckResponse`, `DeductInventoryDto` | `OrderDto`, `Req`, `Resp` |
| Application Validator | PascalCase, hậu tố `Validator` | `CreateOrderRequestValidator`, `DeductInventoryCommandValidator` | `OrderValidator`, `CreateOrderValidator` |
| Infrastructure Repository Implementation | PascalCase, hậu tố `Repository`, đặt trong `Infrastructure/Persistence/` | `OrderRepository`, `OutboxRepository`, `ProductRepository` | `OrderDao`, `OutboxDao`, `EfOrderRepository` |
| Infrastructure DbContext | PascalCase, hậu tố `Context` | `PostgreSQLPrimaryContext`, `ReadReplicaContext` | `MainDbContext`, `WriteDbContext`, `AppDbContext` |
| Infrastructure External Service | PascalCase, hậu tố `Service` | `RedisRateLimitService`, `RabbitMqPublisherService`, `LuaScriptExecutorService` | `RateLimiter`, `ThrottlingService`, `RedisService` |
| Api Controller | PascalCase, hậu tố `Controller`, đặt trong `Api/Controllers/` | `FlashSaleController`, `InventoryController`, `HealthController` | `FlashSaleApi`, `InventoryApi`, `GatewayController` |
| Api Middleware | PascalCase, hậu tố `Middleware` | `RateLimitMiddleware`, `ExceptionMiddleware`, `CorrelationIdMiddleware` | `ThrottlingMiddleware`, `RateLimiterMiddleware` |
| Background Service / Worker | PascalCase, hậu tố `Service` hoặc `Worker` | `AsyncWorkerService`, `OutboxPublisherService` | `Worker`, `BackgroundJob`, `Async_worker` |
| MassTransit Consumer | PascalCase, hậu tố `Consumer` | `OrderCreatedConsumer`, `InventoryDeductedConsumer`, `DeadLetterQueueConsumer` | `OrderConsumer`, `DlqConsumer`, `FailedMessageConsumer` |
| Policy | PascalCase, hậu tố `Policy` | `ExponentialBackoffPolicy`, `RateLimitPolicy` | `BackoffPolicy`, `RetryPolicy` |

### 1.3 Tên method / property / event
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Method hành động | PascalCase, bắt đầu bằng động từ, dùng thuật ngữ glossary | `DeductInventory`, `PublishOrderToQueue`, `CheckRateLimit`, `ProcessDeadLetterQueue`, `RetryWithExponentialBackoff`, `ReconcileOutbox` | `ReduceStock`, `SendOrder`, `LimitRequests`, `ProcessDlq`, `BackoffRetry`, `CheckStock` |
| Property | PascalCase | `Inventory`, `OrderId`, `OutboxId`, `RateLimitState`, `QueueDepth`, `DlqSize`, `InventoryRemaining` | `stock`, `qty`, `order_item_id`, `throttle`, `dlq`, `backoff` |
| Event | PascalCase, hậu tố `Event` hoặc thì quá khứ | `OrderCreatedEvent`, `InventoryDeductedEvent`, `RateLimitExceededEvent` | `OrderEvent`, `StockEvent`, `ThrottleEvent` |
| Boolean property | PascalCase, bắt đầu bằng Is/Has/Can | `IsProcessed`, `HasInventory`, `CanDeduct` | `Processed`, `InventoryOk`, `Deductable` |

### 1.4 Tên biến / tham số / trường private
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Biến cục bộ | camelCase, thuật ngữ glossary | `inventory`, `order`, `outbox`, `rateLimit`, `queueDepth`, `dlqSize`, `inventoryRemaining`, `correlationId` | `stock`, `qty`, `throttle`, `dlq`, `backoff`, `corrId` |
| Trường private | camelCase, prefix `_` | `_inventoryService`, `_outboxRepository`, `_rateLimitPolicy`, `_postgreSQLPrimaryContext`, `_orderRepository` | `_stockService`, `_orderDao`, `_throttleMiddleware` |
| Tham số method | camelCase | `orderId`, `productId`, `message`, `correlationId`, `command`, `query` | `OrderId`, `Product_ID`, `msg`, `cid`, `cmd` |
| Hằng số | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT = 3`, `DEFAULT_RATE_LIMIT_RPS`, `OUTBOX_PURGE_DAYS` | `maxRetry`, `defaultRateLimit`, `outboxPurgeDays` |
| Enum | PascalCase, thành viên PascalCase | `MessageStatus.Published`, `MessageStatus.Failed`, `MessageStatus.MovedToDeadLetterQueue` | `MESSAGE_STATUS_PUBLISHED`, `DLQ`, `FAILED` |

### 1.5 Tên database table / column
Quy tắc: Chọn 1 kiểu (PascalCase hoặc snake_case) cho toàn DB, ghi vào `docs/decisions.md` nếu thay đổi. Ví dụ dưới dùng snake_case.

| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Bảng | snake_case, trùng glossary | `order`, `outbox`, `product`, `flash_sale` | `orders`, `order_items`, `outbox_table`, `event_store`, `audit_log`, `purchases`, `transactions` |
| Cột | snake_case | `id`, `order_id`, `event_type`, `payload`, `occurred_on`, `processed_on`, `correlation_id` | `OrderId`, `EventType`, `Payload`, `order_item_id`, `event_store_id` |
| Khóa chính | `id` | `id` | `OrderId`, `ID`, `order_id` |
| Khóa ngoại | `{table}_id` | `order_id`, `product_id`, `outbox_id` | `OrderId`, `FK_Order`, `orderid` |
| Index | `idx_{table}_{column}` | `idx_order_product_id`, `idx_outbox_processed_on` | `IX_Order_ProductId`, `idx_1` |

### 1.6 Tên API endpoint
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Route path | kebab-case, `/api/v1/{resource}/{action}` | `/api/v1/flash-sale/buy`, `/api/v1/inventory/{productId}`, `/api/v1/health`, `/api/v1/metrics` | `/api/v1/flashsale/buy`, `/api/v1/stock/{id}`, `/api/v1/status`, `/api/v1/Order/Create` |
| Controller | PascalCase, hậu tố `Controller` | `FlashSaleController`, `InventoryController`, `HealthController`, `MetricsController` | `FlashSaleApi`, `InventoryApi`, `GatewayController` |
| Action method | PascalCase, động từ | `Buy`, `CheckInventory`, `GetHealth`, `GetMetrics` | `buy`, `check_inventory`, `Get_Health` |
| Header | Kebab-case hoặc PascalCase chuẩn HTTP | `Correlation-Id`, `Content-Type`, `Authorization` | `correlation_id`, `contenttype`, `Auth` |
| Query param | camelCase | `productId`, `correlationId`, `fromDate`, `toDate` | `product_id`, `ProductId`, `from_date` |

### 1.7 Tên branch git / commit message
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Branch | `{type}/{ma-fr}-{mo-ta-ngan}` | `feature/fr-01-rate-limit`, `fix/fr-02-inventory-deduct`, `chore/nfr-08-docker-compose` | `feature/rate-limit`, `fix/inventory`, `chore/docker` |
| Commit | `{type}: {ma-fr} {mo-ta}` | `feat: FR-01 implement RateLimit middleware`, `fix: FR-02 atomic inventory deduct`, `docs: update conventions.md` | `fix bug`, `update code`, `FR-01 done` |

Các loại commit: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`.

## 2. Cấu trúc thư mục (Clean Architecture: Domain → Application → Infrastructure → Api)

Gốc repo: `E:\AI\.net\agent\FlashSale\`

```
src/
  FlashSale.Domain/                  # Domain layer - không phụ thuộc layer nào
    Entities/                        # Entity nghiệp vụ: Order.cs, Outbox.cs, Product.cs, Inventory.cs, FlashSale.cs
    Events/                          # Domain Event: OrderCreatedEvent.cs, InventoryDeductedEvent.cs
    Exceptions/                      # Domain Exception: InsufficientInventoryException.cs, RateLimitExceededException.cs
    Interfaces/                      # Repository interface: IOrderRepository.cs, IOutboxRepository.cs, IProductRepository.cs
    ValueObjects/                    # Value object nếu có
  FlashSale.Application/             # Application layer - phụ thuộc Domain
    Commands/                        # CQRS Command: DeductInventoryCommand.cs, CreateOrderCommand.cs
    CommandHandlers/                 # Command Handler: DeductInventoryCommandHandler.cs, CreateOrderCommandHandler.cs
    Queries/                         # CQRS Query: GetInventoryQuery.cs, GetOrderQuery.cs
    QueryHandlers/                   # Query Handler: GetInventoryQueryHandler.cs
    DTOs/                            # Data Transfer Object: CreateOrderRequest.cs, OrderResponse.cs
    Validators/                      # FluentValidation: CreateOrderRequestValidator.cs, DeductInventoryCommandValidator.cs
    Interfaces/                      # Application service interface: IOrderService.cs, IInventoryService.cs, IRateLimitService.cs
    Services/                        # Application service: OrderService.cs, InventoryService.cs, RateLimitService.cs
    Mappings/                        # AutoMapper profile nếu dùng
  FlashSale.Infrastructure/          # Infrastructure layer - phụ thuộc Domain + Application
    Persistence/                     # DbContext, Migration, Repository implementation
      PostgreSQLPrimaryContext.cs
      ReadReplicaContext.cs
      Migrations/
      OrderRepository.cs
      OutboxRepository.cs
      ProductRepository.cs
    ExternalServices/                # Dịch vụ bên ngoài
      Redis/                         # Lua Script, StackExchange.Redis
        Scripts/                     # rate_limit_token_bucket.lua, atomic_inventory_deduct.lua
        RedisRateLimitService.cs
        RedisInventoryService.cs
      RabbitMQ/                      # MassTransit
        RabbitMqPublisherService.cs
        MassTransitConfig.cs
    OutboxProcessor/                 # Transactional Outbox publisher
      OutboxPublisherService.cs
  FlashSale.Api/                     # API layer - phụ thuộc Application + Infrastructure
    Controllers/                     # FlashSaleController.cs, InventoryController.cs, HealthController.cs, MetricsController.cs
    Middleware/                      # RateLimitMiddleware.cs, ExceptionMiddleware.cs, CorrelationIdMiddleware.cs
    Program.cs
    appsettings.json
    appsettings.Development.json
    appsettings.Staging.json
    appsettings.Production.json
    DependencyInjection.cs           # Register services cho Api
  FlashSale.Worker/                  # Async Worker (.NET Background Service) - phụ thuộc Application + Infrastructure
    Consumers/                       # MassTransit Consumer: OrderCreatedConsumer.cs, InventoryDeductedConsumer.cs, DeadLetterQueueConsumer.cs
    Services/                        # AsyncWorkerService.cs, OutboxPublisherService.cs
    Program.cs
    appsettings.json
tests/
  FlashSale.Domain.Tests/
    Unit/
  FlashSale.Application.Tests/
    Unit/
  FlashSale.Infrastructure.Tests/
    Integration/
  FlashSale.Api.Tests/
    Integration/
  FlashSale.Worker.Tests/
    Integration/
  Integration.Tests/                 # End-to-end, k6 scripts
docs/
  prd.md, domain-glossary.md, architecture.md, ux.md, conventions.md, decisions.md, status.md, deploy.md, finish-checklist.md, copilot-instructions.md
docker-compose.yml                   # NFR-08: khởi tạo toàn bộ dịch vụ qua DockerCompose
Dockerfile.Api
Dockerfile.Worker
```

Quy tắc phụ thuộc (Dependency Rule):
- `Domain` KHÔNG tham chiếu bất kỳ project nào khác trong solution.
- `Application` chỉ tham chiếu `Domain`.
- `Infrastructure` tham chiếu `Domain` và `Application`.
- `Api` tham chiếu `Application` và `Infrastructure`.
- `Worker` tham chiếu `Application` và `Infrastructure`.
- Không được phép vi ngược (ví dụ: `Infrastructure` không được gọi `Api`, `Domain` không được gọi `Infrastructure`).

Map FR/NFR vào layer:
- FR-01 (RateLimit) → `Application/Services/RateLimitService.cs` + `Infrastructure/ExternalServices/Redis/RedisRateLimitService.cs` + `Api/Middleware/RateLimitMiddleware.cs`
- FR-02 (Inventory) → `Application/Services/InventoryService.cs` + `Infrastructure/ExternalServices/Redis/RedisInventoryService.cs` + `Api/Controllers/InventoryController.cs`
- FR-03 (OrderPipeline) → `Application/Commands/CreateOrderCommand.cs` + `Infrastructure/ExternalServices/RabbitMQ/RabbitMqPublisherService.cs` + `Api/Controllers/FlashSaleController.cs`
- FR-04 (Order + Outbox) → `Application/CommandHandlers/CreateOrderCommandHandler.cs` + `Infrastructure/Persistence/OrderRepository.cs` + `Infrastructure/Persistence/OutboxRepository.cs`
- FR-05 (Retry + DLQ) → `Infrastructure/ExternalServices/RabbitMQ/` + `Worker/Consumers/`
- FR-06 (HealthCheck + Metric) → `Api/Controllers/HealthController.cs` + `Api/Controllers/MetricsController.cs`

## 3. Coding style theo ngôn ngữ (.NET 8 / C#)

### 3.1 C# quy tắc bắt buộc
- Sử dụng C# 12, .NET 8.
- Không dùng `var` cho kiểu rõ ràng (int, string, bool, DateTime), chỉ dùng `var` cho kiểu phức tạp (LINQ, generic).
- Property dùng expression-bodied khi chỉ get đơn giản: `public string Name => _name;`.
- Không dùng region (`#region`) trừ khi cần nhóm rộng lớn (>200 dòng).
- Không dùng `dynamic` trừ khi thực sự cần (liên hệ integration external).
- Không dùng `goto`, `continue` trong vòng lặp lồng nhau > 2 cấp.
- Không dùng `Thread.Sleep`, `Task.Run` không cần thiết trong ASP.NET Core request pipeline.
- Sử dụng `async/await` toàn bộ pipeline I/O (DB, Redis, RabbitMQ, HTTP).
- Không chặn luồng bằng `.Result` hoặc `.Wait()`.
- Sử dụng `ConfigureAwait(false)` trong thư viện (không phải Controller/Service xử lý request trực tiếp).
- Sử dụng `ILogger` từ Microsoft.Extensions.Logging, không dùng `Console.WriteLine` trong production code.
- Sử dụng `System.Text.Json` cho serialization, các property JSON dùng camelCase.
- Sử dụng `Entity Framework Core` cho PostgreSQL, không dùng Dapper trừ khi có quyết định trong `docs/decisions.md`.
- Sử dụng `MassTransit` cho RabbitMQ consumer/producer, không dùng `RabbitMQ.Client` trực tiếp trừ khi có quyết định trong `docs/decisions.md`.
- Sử dụng `StackExchange.Redis` cho Redis, không dùng `ServiceStack.Redis` hay `Microsoft.Extensions.Caching.StackExchangeRedis` trừ khi có quyết định trong `docs/decisions.md`.
- Sử dụng `FluentValidation` cho validation của Command/Query/Request.
- Sử dụng `MediatR` cho CQRS (Command/Query dispatching) trong `Application` và `Api`, không gọi Handler trực tiếp từ Controller nếu không qua MediatR.

### 3.2 Domain Layer
- Entity KHÔNG chứa thuộc tính navigation quá phức tạp, giữ đơn giản.
- Domain Event phải là record hoặc class immutable, chỉ chứa data.
- Domain Exception KHÔNG chứa logic HTTP status code (chỉ message lỗi).
- Repository Interface trong `Domain/Interfaces/` KHÔNG chứa implementation chi tiết EF Core.
- Không dùng `DbContext`, `DbSet`, `Entity Framework` trong `Domain`.

### 3.3 Application Layer
- Command/Query Handler KHÔNG chứa business logic phức tạp, chỉ orchestrate Domain Service và Repository.
- Validator dùng FluentValidation, đặt trong `Validators/`.
- DTO dùng cho truyền dữ liệu giữa Api và Application, không chứa business logic.
- Service trong `Application/Services/` dùng cho logic dùng chung giữa nhiều Command/Query Handler.
- Không dùng `DbContext` trực tiếp trong `Application`, phải qua Repository Interface.

### 3.4 Infrastructure Layer
- Repository Implementation trong `Infrastructure/Persistence/` dùng EF Core.
- `PostgreSQLPrimaryContext` dùng cho ghi (Order, Outbox). `ReadReplicaContext` dùng cho đọc (nếu có).
- External Service (Redis, RabbitMQ) trong `Infrastructure/ExternalServices/`.
- Transactional Outbox: ghi Order và Outbox trong cùng 1 transaction DbContext.
- Không dùng raw SQL trừ khi có quyết định trong `docs/decisions.md`.
- Không gọi `Api` project từ `Infrastructure`.

### 3.5 Api Layer
- Controller KHÔNG chứa business logic. Chỉ nhận request, gọi MediatR/Service, trả response.
- Controller KHÔNG gọi trực tiếp DbContext, Redis, RabbitMQ. Gọi qua Application Service hoặc MediatR.
- Controller KHÔNG chứa hardcoded connection string, secret, API key.
- Mỗi action phải có `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` rõ ràng.
- Mỗi action phải trả `IActionResult` hoặc `ActionResult<T>`, không trả `void`.
- Mỗi action phải có `[ProducesResponseType]` cho status code chính (200, 202, 400, 429, 500).
- Route dùng kebab-case theo Convention 1.6.
- Dependency Injection setup trong `DependencyInjection.cs`.

### 3.6 Worker Layer (AsyncWorker)
- Worker là .NET Background Service, chạy độc lập từ Api.
- Consumer kế thừa `IConsumer<TMessage>` từ MassTransit.
- Consumer chỉ nhận message, gọi Application Service, không chứa business logic phức tạp.
- Consumer phải ack message sau khi xử lý thành công, nack/retry sau khi lỗi.
- Không dùng `console.log`/`Console.WriteLine` trong consumer.

### 3.7 Lua Script (Redis)
- File `.lua` đặt trong `Infrastructure/ExternalServices/Redis/Scripts/`, tên snake_case theo Convention 1.1.
- Lua script phải nguyên tử (atomic), không chia thành nhiều lệnh Redis riêng lẻ cho cùng một tác vụ.
- Không dùng Lua script cho logic phức tạp > 50 dòng (nếu phức tạp hơn, chuyển sang C# service + Redis transaction).

### 3.8 Message Contract (Application/DTOs hoặc Common)
- Mỗi message class đặt trong `Application/DTOs/` hoặc `Common/Contracts/`, tên PascalCase, hậu tố `Message` theo Convention 1.2.
- Mỗi message phải có: `CorrelationId` (Guid), `MessageId` (Guid), `OccurredOn` (DateTimeOffset), `Payload` (object).
- Không chứa secret, PII, mật khẩu trong message payload.
- Không chứa business logic trong message class (chỉ data contract).

### 3.9 Docker
- `Dockerfile` dùng multi-stage build (build stage + runtime stage).
- Base image: `mcr.microsoft.com/dotnet/aspnet:8.0` cho runtime, `mcr.microsoft.com/dotnet/sdk:8.0` cho build.
- Không chạy container dưới user `root` (tạo user non-root).
- Không copy file chứa secret (appsettings.Production.json có secret thật) vào image.
- Expose port 8080 (HTTP) hoặc 8443 (HTTPS), không dùng port 80/443 bên trong container.

## 4. Error handling convention

### 4.1 Cách throw / catch
- Không swallow exception mà không log.
- Không dùng `try-catch` rỗng.
- Không dùng `catch (Exception) { }` hoặc `catch (Exception) { throw; }` mà không thêm giá trị (log, wrap).
- Catch cụ thể trước catch chung: `catch (InsufficientInventoryException)` trước `catch (Exception)`.
- Nếu rethrow, phải dùng `throw;` (không dùng `throw ex;`).
- Dùng `throw new ...Exception(...)` với message tiếng Việt rõ ràng, kèm `correlationId`.

### 4.2 Domain exception (đã chốt trong glossary)
| Exception | Nghĩa | HTTP status | FR |
|-----------|-------|-------------|-----|
| `InsufficientInventoryException` | Hết kho khi `AtomicInventoryOffloading` | 400 Bad Request | FR-02 |
| `RateLimitExceededException` | Vượt `RateLimit` qua `TokenBucket` | 429 Too Many Requests | FR-01 |
| `OrderNotFoundException` | Không tìm thấy `Order` | 404 Not Found | — |
| `OutboxProcessingException` | Lỗi xử lý `Outbox` | 500 Internal Server Error | FR-04, NFR-06 |
| `DeadLetterQueueException` | Lỗi khi chuyển vào `DeadLetterQueue` | 500 Internal Server Error | FR-05 |

### 4.3 Cách trả lỗi ra API (Controller)
- Mọi lỗi business phải trả JSON với cấu trúc chuẩn:
```json
{
  "status": "error",
  "code": "INSUFFICIENT_INVENTORY",
  "message": "Sản phẩm đã hết hàng.",
  "correlationId": "guid"
}
```
- Mã lỗi (`code`) dùng UPPER_SNAKE_CASE, trùng tên exception không có hậu tố `Exception`.
- Không trả stack trace chi tiết cho client (chỉ log nội bộ).
- Không trả 500 cho lỗi business (hết hàng, vượt rate limit). Chỉ trả 500 cho lỗi hệ thống (DB sập, broker mất kết nối).
- Không trả 200 cho lỗi. Phải đúng status code theo Convention 1.6.

### 4.4 Silent fail (CẤM)
- CẤM: `try { ... } catch { }`
- CẤM: `try { ... } catch (Exception ex) { _logger.LogError(ex, "..."); }` rồi tiếp tục chạy như không có gì xảy ra (nếu không có quy định fallback rõ ràng).
- CẤM: `Task.Run(() => { ... })` không await, không xử lý lỗi.
- CẤM: `await Task.Delay(...)` thay vì xử lý lỗi thực sự.

### 4.5 Correlation ID
- Mỗi request phải có `Correlation-Id` header. Nếu client không gửi, ApiGateway sinh GUID mới.
- `CorrelationId` phải được truyền qua toàn bộ chuỗi: `Api` → `Application` → `Infrastructure` → `Message` → `Worker` → `PostgreSQLPrimary`.
- Mỗi log entry phải có `CorrelationId`.

## 5. Testing convention

### 5.1 Naming test file / test case
| Loại | Quy tắc | Ví dụ đúng | Ví dụ sai |
|------|---------|------------|-----------|
| Test class | `{ClassUnderTest}Tests` | `InventoryServiceTests`, `RateLimitMiddlewareTests`, `OrderCreatedConsumerTests`, `DeductInventoryCommandHandlerTests` | `InventoryTest`, `TestInventoryService`, `RateLimitTest` |
| Test method | `{MethodUnderTest}_{Scenario}_{ExpectedResult}` | `DeductInventory_WhenStockAvailable_ShouldSucceed`, `DeductInventory_WhenStockEmpty_ShouldThrowInsufficientInventoryException`, `CheckRateLimit_WhenOverLimit_ShouldReturn429` | `Test1`, `TestDeductInventory`, `ShouldDeduct` |
| Test file | `{ClassUnderTest}Tests.cs` | `InventoryServiceTests.cs` | `inventory_test.cs`, `TestInventory.cs` |

### 5.2 Cấu trúc test (Arrange-Act-Assert)
Mỗi test method phải tuân theo AAA:
```csharp
[Fact]
public void DeductInventory_WhenStockAvailable_ShouldSucceed()
{
    // Arrange
    var inventory = 100;
    var quantity = 10;
    var service = new InventoryService(_repositoryMock.Object);

    // Act
    var result = service.DeductInventory(inventory, quantity);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.RemainingInventory.Should().Be(90);
}
```
- Không kết hợp Arrange và Act trong cùng 1 dòng.
- Không có nhiều Act trong 1 test (nếu cần, chia thành nhiều test).
- Assert phải cụ thể (không chỉ `Assert.True(result)` nếu có thể kiểm tra giá trị).

### 5.3 Mock strategy
- Dùng `Moq` hoặc `NSubstitute` cho tất cả dependency externals (DbContext, Redis, RabbitMQ, HTTP client).
- Không dùng mock cho class trong cùng module nếu không cần thiết (dùng instance thật).
- Mock phải thiết lập `Setup` trước khi gọi Act.
- Verify mock sau khi Act nếu cần kiểm tra side effect (`_repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once)`).
- Không mock static method trừ khi có `Microsoft.Fakes` hay `Moq + Mock.Static` và có quyết định trong `docs/decisions.md`.

### 5.4 Test classification
| Loại | Vị trí | Mục đích | Công cụ |
|------|--------|----------|---------|
| Unit | `tests/FlashSale.Domain.Tests/Unit/`, `tests/FlashSale.Application.Tests/Unit/` | Test class/service độc lập, không có DB, không có Redis, không có RabbitMQ | xUnit + Moq + FluentAssertions |
| Integration | `tests/FlashSale.Infrastructure.Tests/Integration/`, `tests/FlashSale.Api.Tests/Integration/`, `tests/FlashSale.Worker.Tests/Integration/` | Test kết hợp Service + DbContext (Testcontainers PostgreSQL), Service + Redis (Testcontainers), Service + RabbitMQ (Testcontainers) | xUnit + Testcontainers + FluentAssertions |
| E2E / Load | `tests/Integration.Tests/` + k6 | Test toàn bộ luồng API → Redis → RabbitMQ → AsyncWorker → PostgreSQL, giả lập 8.000 VirtualUser | k6 + xUnit |

### 5.5 Coverage tối thiểu (tương xứng với NFR-06 Data Consistency 100%, NFR-03 Error Rate < 1%)
| Loại test | Coverage tối thiểu | Độ nghiêm ngặt |
|-----------|-------------------|----------------|
| Unit (Service, Middleware, Consumer, CommandHandler) | 80% | Bắt buộc pass trước khi commit |
| Integration (DbContext, Redis, RabbitMQ) | 70% | Bắt buộc pass trước khi deploy staging |
| E2E / Load (k6) | Không có coverage số, phải đạt NFR-01 (p99 < 20ms), NFR-03 (Error Rate < 1%) | Bắt buộc pass trước khi deploy production |

### 5.6 Test data
- Không dùng dữ liệu thật từ production trong test.
- Dùng dữ liệu giả (fixture) trong `tests/*/Tests/Fixtures/`.
- Không hardcode ID, email, số thẻ, mật khẩu trong test.

## 6. Quy tắc review (checklist tự chấm trước khi báo "done")

Agent phải tự trả lời đúng/sai mỗi câu dưới đây trước khi báo "done" cho task:

| # | Checklist câu hỏi | Đúng/Sai | Ghi chú |
|---|------------------|----------|---------|
| 6.1 | Mọi tên class, interface, method, biến, bảng, cột, queue, exchange, routing key, metric, endpoint có trùng khớp `docs/domain-glossary.md` không? | | |
| 6.2 | Không có từ trong cột "KHÔNG được gọi là" của glossary xuất hiện trong code không? | | |
| 6.3 | Không có `GatewayController`, `StockService`, `WorkerConsumer`, `EventStoreRepository`, `ThrottlingMiddleware`, `RateLimiter`, `DlqProcessor`, `BackoffPolicy` không? | | |
| 6.4 | Không có `orders`, `order_items`, `outbox_table`, `event_store`, `audit_log` làm tên bảng DB không? | | |
| 6.5 | Không có `order_queue`, `dlq_queue`, `dead_letter_queue`, `failed_messages` làm tên Queue/Exchange không? | | |
| 6.6 | Không có `HTTP_202`, `DLQ`, `FAILED` làm hằng số/enum không? | | |
| 6.7 | Không có `FlashSale.Gateway`, `FlashSale.Backend`, `FlashSale.Worker`, `FlashSale.Stock` làm namespace không? | | |
| 6.8 | Mọi class/module/service quan trọng có thể trace về ít nhất 1 FR hoặc NFR trong `docs/prd.md` không? | | |
| 6.9 | Không hardcode secret (connection string, API key, password, token) trong code không? | | |
| 6.10 | Không có `Console.WriteLine` còn sót trong production code không? | | |
| 6.11 | Không có `TODO`, `FIXME`, `HACK`, `XXX` chưa giải quyết không? | | |
| 6.12 | Không có `try-catch` rỗng hoặc swallow exception không log không? | | |
| 6.13 | Controller không chứa business logic, không gọi DbContext/Redis/RabbitMQ trực tiếp không? | | |
| 6.14 | Mọi action controller có `[ProducesResponseType]` cho status code chính không? | | |
| 6.15 | Mọi test method tuân theo AAA, có tên theo Convention 5.1 không? | | |
| 6.16 | Coverage unit test đạt ít nhất 80% cho module vừa thay đổi không? | | |
| 6.17 | Tất cả unit test pass không? | | |
| 6.18 | Tất cả integration test pass không? | | |
| 6.19 | Nếu thay đổi DB, có migration file theo Convention 1.1 không? | | |
| 6.20 | Nếu thay đổi migration, có kế hoạch rollback rõ ràng không? | | |
| 6.21 | Dependency Rule Clean Architecture được tuân thủ (Domain không tham chiếu layer khác, v.v.) không? | | |
| 6.22 | `docs/status.md` đã được cập nhật trạng thái FR/NFR liên quan chưa? | | |
| 6.23 | Nếu có quyết định kỹ thuật mới, đã ghi vào `docs/decisions.md` chưa? | | |
| 6.24 | Nếu có thuật ngữ mới, đã bổ sung vào `docs/domain-glossary.md` chưa? | | |

Nếu câu trả lời của bất kỳ câu nào là "Sai", KHÔNG được báo "done".

## 7. Điều cấm tuyệt đối

Dưới đây là pattern bị cấm rõ ràng trong dự án này. Violation = phải sửa trước khi commit.

| # | Pattern bị cấm | Lý do |
|---|---------------|-------|
| 7.1 | Business logic trong Controller (Api) | Controller chỉ nhận request, gọi Service/MediatR, trả response |
| 7.2 | Gọi DbContext, Redis, RabbitMQ trực tiếp từ Controller | Phải gọi qua Application Service hoặc MediatR |
| 7.3 | Đặt tên entity/class nghiệp vụ khác với `docs/domain-glossary.md` (ví dụ: `Stock`, `OrderItem`, `Purchase`, `Transaction`, `HoaDon`, `EventStore`, `AuditLog`) | Phải truy ngược về glossary |
| 7.4 | Dùng từ trong cột "KHÔNG được gọi là" của glossary (ví dụ: `Gateway`, `Worker`, `Throttling`, `RateLimiter`, `Dlq`, `Master`, `Slave`, `OutboxTable`) | Ngôn ngữ không nhất quán |
| 7.5 | Hardcode secret (connection string, API key, password, token) trong code | Bảo mật NFR-04 |
| 7.6 | Swallow exception không log (`try-catch` rỗng) | Khó debug, mất lỗi |
| 7.7 | `Console.WriteLine` trong production code | Không có cấu trúc log |
| 7.8 | `.Result` hoặc `.Wait()` chặn luồng async | Tác động đến p99 latency NFR-01 |
| 7.9 | Trả 200 cho lỗi business (hết hàng, vượt rate limit) | Phải đúng HTTP status code (400, 429) |
| 7.10 | Trả stack trace chi tiết cho client | Rò rỉ thông tin nội bộ |
| 7.11 | Dùng `dynamic` trừ khi có quyết định trong `docs/decisions.md` | Kiểm tra kiểu hạn chế |
| 7.12 | Dùng raw SQL trừ khi có quyết định trong `docs/decisions.md` | SQL injection, bảo trì khó |
| 7.13 | Chạy container dưới user `root` | Bảo mật container |
| 7.14 | Copy file chứa secret thật vào Docker image | Bảo mật NFR-04 |
| 7.15 | Commit file `.env`, `appsettings.Production.json` chứa secret thật vào repo | Bảo mật NFR-04 |
| 7.16 | Deploy lên production mà không qua pipeline test (unit → integration → e2e) | NFR-06 Data Consistency, NFR-03 Error Rate |
| 7.17 | Deploy lên production khi bất kỳ tầng test nào fail | Pipeline phải dừng |
| 7.18 | Dùng `Thread.Sleep` trong request pipeline | Tác động latency |
| 7.19 | Dùng `Task.Run` không cần thiết trong ASP.NET Core request pipeline | Tác động throughput |
| 7.20 | Vi ngược Dependency Rule (ví dụ: Domain tham chiếu Infrastructure, Infrastructure tham chiếu Api) | Clean Architecture |

File này là nguồn chốt quy ước đặt tên và cấu trúc. Mọi thay đổi quy ước phải được ghi vào `docs/decisions.md` và cập nhật lại file này.
