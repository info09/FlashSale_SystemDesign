# Status Log — Nhật ký tiến độ thực hiện (Nguồn sự thật duy nhất)

File này là nguồn sự thật duy nhất về tiến độ dự án. Phải được cập nhật sau **MỖI prompt implement**, không phải cuối ngày hay cuối phase.

Mã FR/NFR không được thay đổi, định dạng FR-XX / NFR-XX theo `docs/prd.md`.
Tên phase, tên hạng mục, tên entity/table/queue PHẢI dùng đúng thuật ngữ trong `docs/domain-glossary.md`.

## 1. Tổng quan phase

Dự án chia thành 7 phase theo thứ tự triển khai (ưu tiên nền tảng trước, tính năng user-facing sau):

| Phase | Tên phase | Mục tiêu | FR/NFR liên quan |
|-------|-----------|----------|------------------|
| Phase 0 | Nền tảng hạ tầng & CI-CD | Khởi tạo Docker Compose, cấu trúc thư mục, secret management, pipeline CI-CD cơ bản | NFR-08 |
| Phase 1 | Traffic Protection (Rate Limit) | Xây dựng ApiGateway kiểm tra RateLimit qua Redis Lua Script (Token Bucket), trả HTTP 429 nếu vượt ngưỡng | FR-01, NFR-04 |
| Phase 2 | Atomic Inventory Offloading | Xây dựng Inventory Module trừ kho atomic trên Redis qua Lua Script, chặn OverSelling | FR-02, NFR-01 |
| Phase 2.5 | Xác thực & Đăng nhập (Identity + Bearer Token) | Tích hợp ASP.NET Core Identity + Bearer Token (JWT) để xác thực client trước khi kiểm tra RateLimit | FR-01, NFR-04 |
| Phase 3 | Event-Driven Order Pipeline | Đẩy Message vào RabbitMQ Queue (OrderPipeline), trả Http202Accepted ngay cho FlashSaleShopper | FR-03 |
| Phase 4 | Async Worker & Transactional Outbox | AsyncWorker ghi Order + Outbox vào PostgreSQLPrimary trong 1 transaction, Retry ExponentialBackoff, DLQ sau 3 lần | FR-04, FR-05, NFR-06 |
| Phase 5 | Observability & Monitoring | Endpoint HealthCheck, xuất Metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) cho Prometheus/Grafana | FR-06, NFR-07 |
| Phase 6 | Load Testing & Benchmarking | Bộ k6 Load Test giả lập 8.000 VirtualUser, đo p99 latency < 20ms, Error Rate < 1%, spike 15.000 RPS | FR-07, NFR-01, NFR-02, NFR-03 |
| Phase 7 | UI / Frontend (nếu có) | Xây dựng giao diện theo docs/ux.md (hiện tại ngoài phạm vi MVP, chỉ kế hoạch) | Theo docs/ux.md |

## 2. Checklist hạng mục theo phase (chia thành stories nhỏ)

Mỗi story là 1 đơn vị việc đủ nhỏ để hoàn thành trong **1 prompt**. Chỉ được tick `- [x]` khi đã có unit test + integration test + e2e test liên quan đều pass (theo coverage đã định trong `docs/conventions.md`). Không tick khi code "chạy được" nhưng chưa có test.

### Phase 0: Nền tảng hạ tầng & CI-CD (NFR-08)

- [x] Story 0.1: Tạo solution (.sln) và cấu trúc thư mục theo Clean Architecture (FlashSale.Domain, FlashSale.Application, FlashSale.Infrastructure, FlashSale.Api, FlashSale.Worker, tests/)
- [x] Story 0.2: Thêm tham chiếu giữa các project tuân theo Dependency Rule (Domain không tham chiếu layer khác, Application chỉ tham chiếu Domain, Infrastructure tham chiếu Domain+Application, Api tham chiếu Application+Infrastructure, Worker tham chiếu Application+Infrastructure)
- [x] Story 0.3: Cấu hình docker-compose.yml chạy toàn bộ dịch vụ: FlashSale.Api, FlashSale.Worker, Redis Cluster, RabbitMQ, PostgreSQLPrimary, ReadReplica, Prometheus, Grafana, k6
- [x] Story 0.3b (sửa lỗi): Sửa Dockerfile Api/Worker (đường dẫn COPY sai, thêm .dockerignore), docker compose config pass
- [x] Story 0.4: Thiết lập secret management (.env.example không chứa giá trị thật, biến môi trường cho connection string PostgreSQLPrimary, ReadReplica, Redis, RabbitMQ)
- [x] Story 0.4b (hoàn thiện): Sửa docker-compose.yml dùng ${BIẾN} lấy từ .env thay vì hardcode, cập nhật .env.example đầy đủ biến (bao gồm POSTGRES_*, RABBITMQ_DEFAULT_*), tạo .dockerignore
- [x] Story 0.5: Tạo pipeline CI-CD cơ bản (build .NET 8, chạy unit test, dừng pipeline nếu test fail)
- [x] Story 0.6: Viết integration test kiểm tra kết nối Docker Compose (tất cả service khởi động được, có thể ping nhau)

### Phase 1: Traffic Protection (Rate Limit) — FR-01, NFR-04

- [x] Story 1.1 (Domain): Tạo entity RateLimit, interface IRateLimitRepository, exception RateLimitExceededException trong FlashSale.Domain
- [x] Story 1.2 (Application): Tạo RateLimitService, RateLimitCommand, RateLimitValidator trong FlashSale.Application
- [x] Story 1.3 (Infrastructure): Viết Lua Script TokenBucket (rate_limit_token_bucket.lua), nguyên tử, không chia thành nhiều lệnh; triển khai RedisRateLimitService trong FlashSale.Infrastructure/ExternalServices/Redis/
- [x] Story 1.4 (Api): Tạo RateLimitMiddleware kiểm tra RateLimit trước khi xử lý tiếp, trả HTTP 429 nếu vượt ngưỡng; đăng ký middleware trong Program.cs
- [x] Story 1.5 (Security): Đảm bảo tất cả API endpoint chỉ cho phép gọi qua HTTPS (NFR-04)
- [x] Story 1.6 (Test): Viết unit test cho RateLimitService (mock Redis Lua Script) — đã hoàn thành trong Story 1.2 (5 test case pass)
- [x] Story 1.7 (Test): Viết integration test kiểm tra RateLimit qua Redis thật (Testcontainers)
- [x] Story 1.8 (Test): Viết e2e test kiểm tra luồng HTTP: vượt ngưỡng → nhận 429, dưới ngưỡng → tiếp tục xử lý
- [x] Story 1.9 (Infra): Cấu hình RateLimit phân tán trên Redis Cluster (không lưu local memory trên mỗi instance ApiGateway)

### Phase 2: Atomic Inventory Offloading — FR-02, NFR-01

- [x] Story 2.1 (Domain): Tạo entity Inventory, interface IInventoryRepository, exception InsufficientInventoryException trong FlashSale.Domain
- [x] Story 2.2 (Application): Tạo InventoryService, DeductInventoryCommand, DeductInventoryValidator trong FlashSale.Application
- [x] Story 2.3 (Infrastructure): Viết Lua Script atomic (atomic_inventory_deduct.lua), trừ kho nguyên tử; triển khai RedisInventoryService trong FlashSale.Infrastructure/ExternalServices/Redis/
- [x] Story 2.4 (Api): Tạo InventoryController, endpoint trừ kho trả lỗi hết hàng nếu không đủ kho, không tạo Order
- [x] Story 2.5 (Test): Viết unit test cho InventoryService (mock Redis Lua Script) — 5 test case pass
- [x] Story 2.6 (Test): Viết integration test kiểm tra trừ kho atomic qua Redis thật (Testcontainers) — 4 test case pass
- [x] Story 2.7 (Test): Viết e2e test kiểm tra luồng HTTP: đủ kho → trừ thành công (200), hết hàng → trả lỗi 400, không tạo Order — 2 test case pass (WebApplicationFactory + Testcontainers Redis)
- [x] Story 2.8 (Perf): Đo p99 latency của AtomicInventoryOffloading mục tiêu ~18ms (NFR-01) — P99 = 1ms (1000 iterations, Redis qua Testcontainers), đáp ứng < 20ms

### Phase 2.5: Xác thực & Đăng nhập (Identity + Bearer Token) — FR-01, NFR-04

- [x] Story 2.5.1 (Domain): Tạo entity User, Role, interface IUserRepository, IUserService trong FlashSale.Domain
- [x] Story 2.5.2 (Infrastructure): Cấu hình ASP.NET Core Identity với PostgreSQLPrimary (DbContext, UserRepository, AuthService) — unit test AuthService 6 case pass + integration test UserRepository 6 case pass (PostgreSQL Testcontainers); migration file `20261105005114_CreateIdentityTables.cs` đã tạo (Up/Down reversible, bảng User/Role PascalCase)
- [x] Story 2.5.3 (Infrastructure): Triển khai JwtBearer authentication (AuthenticationBuilder, JwtBearerOptions, TokenValidationParameters), ký JWT token khi đăng nhập thành công — package Microsoft.AspNetCore.Authentication.JwtBearer, JwtTokenService, IJwtTokenService, AddAuthentication + AddJwtBearer trong Program.cs
- [x] Story 2.5.4 (Application): Tạo AuthService, LoginCommand, RegisterCommand, LoginResult, validators trong FlashSale.Application
- [x] Story 2.5.5 (Api): Tạo AuthController, endpoint /api/v1/auth/login, /api/v1/auth/register, bảo vệ endpoint InventoryController bằng [Authorize]
- [x] Story 2.5.6 (Api): Cập nhật RateLimitMiddleware để lấy ClientId từ authenticated user (Claim NameIdentifier) thay vì header X-Client-Id/IP
- [x] Story 2.5.7 (Test): Viết unit test cho AuthService (mock IUserRepository, mock JwtBearer) — 6 test case pass
- [x] Story 2.5.8 (Test): Viết integration test kiểm tra đăng nhập nhận JWT, gọi endpoint cần auth thành công/thất bại (401) — AuthEndToEndTest 6 test case pass (register, login, login wrong password, inventory without token, inventory with valid token)
- [x] Story 2.5.9 (Test): Viết e2e test kiểm tra luồng HTTP: không token → 401, có token hợp lệ → xử lý tiếp — AuthFlowEndToEndTest 1 test case pass (register → login → lấy token → gọi protected endpoint với token)

### Phase 3: Event-Driven Order Pipeline — FR-03

- [x] Story 3.1 (Domain): Tạo entity Order, interface IOrderRepository trong FlashSale.Domain
- [x] Story 3.2 (Application): Tạo CreateOrderCommand, CreateOrderCommandHandler, OrderService, message contract OrderCreatedMessage (có CorrelationId, MessageId, OccurredOn, Payload) trong FlashSale.Application
- [x] Story 3.3 (Infrastructure): Cấu hình RabbitMQ Exchange/Queue/Routing Key (tên chốt trong docs/decisions.md, dùng thuật ngữ OrderPipeline, Queue, Message); triển khai RabbitMqPublisherService trong FlashSale.Infrastructure/ExternalServices/RabbitMQ/
- [x] Story 3.4 (Api): Tạo FlashSaleController, endpoint mua hàng trả Http202Accepted ngay cho FlashSaleShopper sau khi đẩy Message vào Queue, không chờ xử lý xong
- [x] Story 3.5 (Test): Viết unit test cho OrderCreatedMessage và RabbitMqPublisherService (mock RabbitMQ) — 6 test case pass (OrderCreatedMessageTests 3, RabbitMqPublisherServiceTests 3)
- [x] Story 3.6 (Test): Viết integration test kiểm tra publish message vào RabbitMQ thật (Testcontainers) — 2 test case pass (RabbitMqPublisherIntegrationTest)
- [x] Story 3.7 (Test): Viết e2e test kiểm tra luồng HTTP: trừ kho thành công → publish message → nhận 202 Accepted — 2 test case pass (FlashSaleBuyEndToEndTest)

### Phase 4: Async Worker & Transactional Outbox — FR-04, FR-05, NFR-06

- [x] Story 4.1 (Domain): Tạo entity Outbox, interface IOutboxRepository trong FlashSale.Domain — build 0 error
- [] Story 4.2 (Application): Tạo OutboxService, OutboxPublisher trong FlashSale.Application
- [] Story 4.3 (Infrastructure): Tạo PostgreSQLPrimaryContext, ReadReplicaContext, migration cho bảng Order và Outbox; triển khai OrderRepository, OutboxRepository trong FlashSale.Infrastructure/Persistence/
- [] Story 4.4 (Application): Triển khai ghi Order + Outbox vào PostgreSQLPrimary trong cùng 1 transaction (Transactional Outbox Pattern)
- [] Story 4.5 (Worker): Tạo AsyncWorker (.NET 8 Background Service) trong FlashSale.Worker/
- [] Story 4.6 (Worker): Tạo MassTransit consumer OrderCreatedConsumer tiêu thụ Message từ Queue
- [] Story 4.7 (Worker): Triển khai Retry với ExponentialBackoff (tối đa 3 lần) khi xử lý message thất bại
- [] Story 4.8 (Worker): Triển khai chuyển Message vào DeadLetterQueue sau 3 lần thử vẫn lỗi (FR-05)
- [] Story 4.9 (Test): Viết unit test cho OrderService (mock PostgreSQLPrimary, mock OutboxRepository)
- [] Story 4.10 (Test): Viết integration test kiểm tra ghi Order + Outbox trong 1 transaction PostgreSQL thật (Testcontainers)
- [] Story 4.11 (Test): Viết e2e test kiểm tra luồng đầy đủ: ApiGateway → RabbitMQ → AsyncWorker → PostgreSQLPrimary + Outbox
- [] Story 4.12 (Reliability): Viết reconciliation script/process để đối chiếu 100% đơn 202 Accepted đã có bản ghi trong PostgreSQLPrimary (NFR-06)
- [] Story 4.13 (Config): Định nghĩa thời gian purge/archive bảng Outbox ([CẦN LÀM RÕ] trong docs/prd.md Câu 5)

### Phase 5: Observability & Monitoring — FR-06, NFR-07

- [] Story 5.1 (Api): Triển khai endpoint HealthCheck (/health hoặc /api/v1/health) kiểm tra Redis, RabbitMQ, PostgreSQLPrimary
- [] Story 5.2 (Api + Worker): Triển khai xuất Metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) từ ApiGateway và AsyncWorker
- [] Story 5.3 (Infra): Cấu hình Prometheus scrape metric từ cả hai dịch vụ
- [] Story 5.4 (Infra): Cấu hình Grafana Dashboard hiển thị metric trên
- [] Story 5.5 (Test): Viết unit test cho HealthCheck endpoint
- [] Story 5.6 (Test): Viết integration test kiểm tra HealthCheck trả về Healthy/Unhealthy khi dịch vụ lên/lỗi
- [] Story 5.7 (Test): Viết e2e test kiểm tra Grafana Dashboard hiển thị đầy đủ metric sau khi deploy

### Phase 6: Load Testing & Benchmarking — FR-07, NFR-01, NFR-02, NFR-03

- [] Story 6.1 (Test): Viết script k6 Load Test giả lập 8.000 VirtualUser đồng thời
- [] Story 6.2 (Test): Chạy k6 kiểm tra p99 latency luồng nhận đơn < 20ms (NFR-01)
- [] Story 6.3 (Test): Chạy k6 kiểm tra spike 15.000 RPS, không trả về 5xx do quá tải (ngoại 429 rate limiter) (NFR-02)
- [] Story 6.4 (Test): Chạy k6 kiểm tra Error Rate < 1% khi 8.000 VirtualUser (NFR-03)
- [] Story 6.5 (Test): Viết integration test kết hợp k6 với Docker Compose môi trường staging
- [] Story 6.6 (Metric): Đối chiếu kết quả k6 với Success Metrics trong docs/prd.md (không over-selling, không mất đơn hàng)

### Phase 7: UI / Frontend (theo docs/ux.md — ngoài phạm vi MVP)

- [] Story 7.1 (UX): Thiết kế màn hình Gọi API mua hàng (FlashSaleShopper) theo docs/ux.md phần Information Architecture
- [] Story 7.2 (UX): Thiết kế màn hình Grafana Dashboard cho DevOps theo docs/ux.md
- [] Story 7.3 (UX): Triển khai component Form, Table, Modal, Toast, Empty state, Loading skeleton theo docs/ux.md phần 5
- [] Story 7.4 (UX): Áp dụng design tokens (màu, typography, spacing, breakpoint) theo docs/ux.md phần 4 ([ĐỀ XUẤT - CẦN DUYỆT])
- [] Story 7.5 (UX): Đảm bảo Accessibility tối thiểu theo docs/ux.md phần 6
- [] Story 7.6 (UX): Áp dụng Content/tone of voice theo docs/ux.md phần 7 (tiếng Việt, thuật ngữ khớp glossary)
- [] Story 7.7 (Test): Viết unit test cho component UI (nếu có frontend)
- [] Story 7.8 (Test): Viết integration test cho luồng UI gọi API (nếu có frontend)

## 3. Trạng thái hiện tại

- **Phase đang làm:** Phase 0 - Nền tảng hạ tầng & CI-CD
- **Story đang làm:** Tích hợp Scalar API documentation (FR-06, NFR-07 hỗ trợ quan sát API)
- **Lần cập nhật gần nhất:** Tích hợp Scalar API documentation (Swashbuckle.AspNetCore + Scalar.AspNetCore) vào FlashSale.Api, truy cập tại /scalar/v1; thêm app.MapControllers() để đăng ký InventoryController; hoàn thành Story 2.4 (Api): InventoryController, endpoint DeductInventory trả HTTP 200 khi đủ kho, HTTP 400 khi hết kho; hoàn thành Story 2.5 (Test): unit test InventoryService 5 test case pass (mock IInventoryRepository); hoàn thành Story 2.6 (Test): integration test RedisInventoryIntegrationTest 4 test case pass (Testcontainers Redis thật, kiểm tra atomic không over-selling); hoàn thành Story 2.7 (Test): e2e test InventoryEndToEndTest 2 test case pass (WebApplicationFactory + Testcontainers Redis); hoàn thành Story 2.8 (Perf): đo p99 latency AtomicInventoryOffloading = 1ms (1000 iterations), đáp ứng NFR-01 < 20ms; hoàn thành Story 2.5.1 (Domain): User, Role, IUserRepository, IUserService; hoàn thành Story 2.5.2 (Infrastructure): ASP.NET Core Identity + PostgreSQLPrimaryContext, UserRepository, AuthService — unit test AuthService 6 case pass + integration test UserRepository 6 case pass (PostgreSQL Testcontainers); migration file `20261105005114_CreateIdentityTables.cs` đã tạo (Up/Down reversible, bảng User/Role PascalCase); hoàn thành Story 2.5.3 (Infrastructure): JwtBearer authentication (AddAuthentication + AddJwtBearer trong Program.cs), JwtTokenService, IJwtTokenService, package Microsoft.AspNetCore.Authentication.JwtBearer; thêm cấu hình JWT vào appsettings.json, appsettings.Development.json, .env.example (JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE, JWT_EXPIRATION_MINUTES); tích hợp Serilog logging (Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File) với cấu hình Console + File (rolling daily, retained 7 ngày, shared: true để nhiều process ghi cùng file không sinh file _001/_002), enrich FromLogContext/MachineName/ProcessId/ThreadId; hoàn thành Story 2.5.4 (Application): LoginCommand, RegisterCommand, LoginResult, LoginCommandHandler, RegisterCommandHandler, LoginCommandValidator, RegisterCommandValidator, đăng ký MediatR + validators trong DependencyInjection.cs; hoàn thành Story 2.5.5 (Api): AuthController (POST /api/v1/auth/login, POST /api/v1/auth/register, cả hai AllowAnonymous), bảo vệ InventoryController bằng [Authorize] (yêu cầu Bearer Token trước khi trừ kho); hoàn thành ExceptionHandlerMiddleware (bọc toàn bộ exception, trả JSON chuan {status, code, message, correlationId}, khong swallow exception), bo try-catch rong trong RedisScriptExecutor.cs; tạo AdminSeeder (seed Role "Admin" va User "admin" vao PostgreSQLPrimary, idempotent) va goi trong Program.cs sau khi migrate DB; hoàn thành Story 2.5.6 (Api): Cap nhat RateLimitMiddleware lay ClientId tu Claim NameIdentifier cua user da xac thuc thay vi header X-Client-Id/IP; hoàn thành Story 2.5.7 (Test): unit test AuthService 6 test case pass (mock IUserRepository, SignInManager, UserManager); hoàn thành Story 2.5.8 (Test): integration test AuthEndToEndTest 6 test case pass (đăng ký, đăng nhập nhận JWT, sai mật khẩu → 401, không token → bị chặn, có token → gọi được inventory); hoàn thành Story 2.5.9 (Test): e2e test AuthFlowEndToEndTest 1 test case pass (register → login → lấy access token → gọi protected endpoint với token); đổi chuẩn đặt tên DB từ snake_case sang PascalCase (bảng User, Role; cột UserName, Email, v.v.); chốt cơ chế xác thực ASP.NET Core Identity + Bearer Token (JWT) (docs/prd.md + docs/decisions.md); tích hợp Authorization cho Scalar/Swagger (AddSecurityDefinition "Bearer" + AddSecurityRequirement) để nhập JWT Bearer token khi test API qua giao diện; sửa thứ tự middleware: UseAuthentication()/UseAuthorization() chạy TRUOC RateLimitMiddleware de context.User.Identity duoc populate tu JWT token (IsAuthenticated khong con false); build 0 error
- **Sửa lỗi JWT:** RateLimitMiddleware IsAuthenticated=false do ValidIssuer/ValidAudience=null (builder.Configuration không có AddInMemoryCollection từ WebApplicationFactory) → thêm fallback ?? "FlashSale.Api" / ?? "FlashSale.Client"; build 0 error
- **Sửa lỗi RedisScriptExecutor:** Lua script trả về mảng (ví dụ: [allowed, remaining, resetAfter]) làm Result.ToString() trả về "N element(s)" thay vì giá trị thực → dùng reflection để đọc trường _result bên trong RedisResult và chuyển đổi mảng thành chuỗi phân tách bằng dấu phẩy; đồng thời thay đổi rate_limit_token_bucket.lua trả về chuỗi delimiter thay vì mảng Lua; build 0 error
hoàn thành Story 3.1 (Domain): Tạo entity Order, interface IOrderRepository trong FlashSale.Domain
- Hoàn thành Story 3.2 (Application): Tạo CreateOrderCommand, CreateOrderCommandHandler, OrderService, message contract OrderCreatedMessage (có CorrelationId, MessageId, OccurredOn, Payload) trong FlashSale.Application; đăng ký IOrderService, CreateOrderCommandValidator trong DependencyInjection.cs; build 0 error
- Hoàn thành Story 3.3 (Infrastructure): Cấu hình RabbitMQ Exchange/Queue/Routing Key (Exchange: flash_sale_exchange, Queue: order_pipeline_queue, Routing Key: order.created, DLQ: order_pipeline_dlq); triển khai RabbitMqPublisherService trong FlashSale.Infrastructure/ExternalServices/RabbitMQ/; đăng ký IRabbitMqPublisherService trong DependencyInjection.cs; thêm package RabbitMQ.Client 6.6.0; build 0 error
- Hoàn thành Story 3.4 (Api): Tạo FlashSaleController, endpoint /api/v1/flash-sale/buy trả Http202Accepted ngay cho FlashSaleShopper sau khi đẩy Message vào Queue, không chờ xử lý xong; bảo vệ bằng [Authorize]; build 0 error
- Sửa lỗi DI: Tạo OrderRepository trong Infrastructure/Persistence/OrderRepository.cs; thêm DbSet<Order> vào PostgreSQLPrimaryContext; đăng ký IOrderRepository → OrderRepository trong DependencyInjection.cs; build 0 error
- Tạo migration `20261105043932_CreateOrdersTable.cs` cho bảng Order (Id, ProductId, Quantity, ClientId, OrderStatus, CreatedOn) với Up/Down reversible; build 0 error
- Hoàn thành Story 3.5 (Test): Viết unit test cho OrderCreatedMessage (3 case) và RabbitMqPublisherService (3 case, mock RabbitMQ qua Moq) — tất cả 6 test case pass; build 0 error
- Hoàn thành Story 3.6 (Test): Viết integration test RabbitMqPublisherIntegrationTest (2 case) kiểm tra publish message vào RabbitMQ thật qua Testcontainers, verify body JSON và message properties (ContentType, CorrelationId, MessageId, Persistent); build 0 error
- Hoàn thành Story 3.7 (Test): Viết e2e test FlashSaleBuyEndToEndTest (2 case) kiểm tra luồng HTTP: đủ kho → 202 Accepted + Order lưu PostgreSQL + RabbitMQ pipeline hoạt động; hết kho → 400 BUSINESS_ERROR; thêm package Testcontainers.PostgreSql, Testcontainers.RabbitMq, RabbitMQ.Client vào Integration.Tests.csproj; build 0 error, 14/14 Integration.Tests pass
- Hoàn thành Story 4.1 (Domain): Tạo entity Outbox (src/FlashSale.Domain/Entities/Outbox.cs) và interface IOutboxRepository (src/FlashSale.Domain/Interfaces/IOutboxRepository.cs) cho Transactional Outbox Pattern (FR-04, NFR-06); build 0 error

## 4. Deferred Work (việc đã cố ý hoãn)

| Việc bị hoãn | Hoãn từ story/phase | Lý do hoãn | Mốc quay lại |
|--------------|---------------------|------------|--------------|
| Xây dựng UI/Frontend thực tế | Phase 7 (tất cả story) | Ngoài phạm vi MVP (theo docs/prd.md và docs/architecture.md), chỉ tập trung Backend API Core & Pipeline | Chưa xác định, cần review lại trước khi release |
| Chi tiết deploy produzione (cloud provider, load balancer, tên miền, số instance) | Phase 0 (Story 0.3) | docs/deploy.md chưa được tạo (chờ xác nhận hosting/infra từ người dùng) | Chưa xác định, cần tạo docs/deploy.md trước |
| Cơ chế xác thực client (ASP.NET Core Identity + Bearer Token JWT) | Phase 1 (Story 1.4, 1.5) | Đã chốt: ASP.NET Core Identity + Bearer Token (docs/prd.md + docs/decisions.md) | Phase 2.5 |
| Tên RabbitMQ Exchange/Queue/Routing Key cụ thể | Phase 3 (Story 3.3) | [CẦN LÀM RÕ] trong docs/prd.md Câu 2 | Chưa xác định |
| Thời gian purge/archive bảng Outbox | Phase 4 (Story 4.13) | [CẦN LÀM RÕ] trong docs/prd.md Câu 5 | Chưa xác định |
| Thời gian giữ message trong DeadLetterQueue | Phase 4 (Story 4.8) | [CẦN LÀM RÕ] trong docs/prd.md Câu 6 | Chưa xác định |
| Replication lag acceptable Primary→ReadReplica | Phase 0/4 (Story 0.3, 4.3) | [CẦN LÀM RÕ] trong docs/prd.md Câu 4 | Chưa xác định |

## 5. Quy tắc cập nhật file này

Sau khi hoàn thành bất kỳ story nào, phải tick checkbox tương ứng trong file này **TRƯỚC KHI** báo hoàn thành task cho người dùng.

- Nếu story chưa đủ test 3 tầng (unit/integration/e2e) thì không được tick, dù code đã chạy đúng.
- Nếu trong lúc làm phát sinh việc cần hoãn lại, phải thêm ngay vào Deferred Work (bảng ở mục 4), không được bỏ qua âm thầm.
- Nếu trong lúc làm phát sinh quyết định đáng ghi, phải thêm vào `docs/decisions.md` trước, rồi mới tick checklist ở đây.
- Mỗi lần cập nhật phải ghi vào mục 3 (Trạng thái hiện tại) phase và story đang làm.

## 6. Blocker/Risk log

| # | Vướng mắc / Rủi ro | Liên quan FR/NFR | Cần quyết định từ người dùng | Ngày ghi |
|---|-------------------|------------------|------------------------------|----------|
| 1 | docs/deploy.md chưa tồn tại, chi tiết hosting/infra/chính sách deploy chưa chốt | NFR-08, Phase 0 | Cloud provider, số môi trường, CI/CD tool, load balancer, tên miền, số instance production | |
| 2 | Cơ chế xác thực client chưa chốt | FR-01, NFR-04, Phase 1 | API key, JWT, hoặc không auth trong MVP | |
| 3 | Tên RabbitMQ Exchange/Queue/Routing Key chưa chốt | FR-03, Phase 3 | Tên cụ thể cho pipeline tạo đơn | |
| 4 | Thời gian purge/archive Outbox chưa chốt | NFR-06, Phase 4 | Thời gian giữ bản ghi trước khi purge | |
| 5 | Thời gian giữ message trong DeadLetterQueue chưa chốt | FR-05, NFR-07, Phase 4 | Thời gian giữ message trước khi xóa/can thiệp | |
| 6 | Replication lag acceptable Primary→ReadReplica chưa chốt | NFR-06, Phase 0/4 | Thời gian chênh lệch chấp nhận được | |
