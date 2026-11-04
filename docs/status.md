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
- [] Story 1.9 (Infra): Cấu hình RateLimit phân tán trên Redis Cluster (không lưu local memory trên mỗi instance ApiGateway)

### Phase 2: Atomic Inventory Offloading — FR-02, NFR-01

- [] Story 2.1 (Domain): Tạo entity Inventory, interface IInventoryRepository, exception InsufficientInventoryException trong FlashSale.Domain
- [] Story 2.2 (Application): Tạo InventoryService, DeductInventoryCommand, DeductInventoryValidator trong FlashSale.Application
- [] Story 2.3 (Infrastructure): Viết Lua Script atomic (atomic_inventory_deduct.lua), trừ kho nguyên tử; triển khai RedisInventoryService trong FlashSale.Infrastructure/ExternalServices/Redis/
- [] Story 2.4 (Api): Tạo InventoryController, endpoint trừ kho trả lỗi hết hàng nếu không đủ kho, không tạo Order
- [] Story 2.5 (Test): Viết unit test cho InventoryService (mock Redis Lua Script)
- [] Story 2.6 (Test): Viết integration test kiểm tra trừ kho atomic qua Redis thật (Testcontainers)
- [] Story 2.7 (Test): Viết e2e test kiểm tra luồng HTTP: đủ kho → trừ thành công, hết hàng → trả lỗi 400, không tạo Order
- [] Story 2.8 (Perf): Đo p99 latency của AtomicInventoryOffloading mục tiêu ~18ms (NFR-01)

### Phase 3: Event-Driven Order Pipeline — FR-03

- [] Story 3.1 (Domain): Tạo entity Order, interface IOrderRepository trong FlashSale.Domain
- [] Story 3.2 (Application): Tạo CreateOrderCommand, CreateOrderCommandHandler, OrderService, message contract OrderCreatedMessage (có CorrelationId, MessageId, OccurredOn, Payload) trong FlashSale.Application
- [] Story 3.3 (Infrastructure): Cấu hình RabbitMQ Exchange/Queue/Routing Key (tên chốt trong docs/decisions.md, dùng thuật ngữ OrderPipeline, Queue, Message); triển khai RabbitMqPublisherService trong FlashSale.Infrastructure/ExternalServices/RabbitMQ/
- [] Story 3.4 (Api): Tạo FlashSaleController, endpoint mua hàng trả Http202Accepted ngay cho FlashSaleShopper sau khi đẩy Message vào Queue, không chờ xử lý xong
- [] Story 3.5 (Test): Viết unit test cho OrderCreatedMessage và RabbitMqPublisherService (mock RabbitMQ)
- [] Story 3.6 (Test): Viết integration test kiểm tra publish message vào RabbitMQ thật (Testcontainers)
- [] Story 3.7 (Test): Viết e2e test kiểm tra luồng HTTP: trừ kho thành công → publish message → nhận 202 Accepted

### Phase 4: Async Worker & Transactional Outbox — FR-04, FR-05, NFR-06

- [] Story 4.1 (Domain): Tạo entity Outbox, interface IOutboxRepository trong FlashSale.Domain
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
- **Story đang làm:** Story 1.9 (Infra): Cấu hình RateLimit phân tán trên Redis Cluster (không lưu local memory trên mỗi instance ApiGateway)
- **Lần cập nhật gần nhất:** Sửa test case RateLimitServiceTests (chuyển từ ThrowAsync<RateLimitExceededException> sang trả về RateLimitResult.IsAllowed = false cho phù hợp với việc Service trả về Result thay vì ném exception), tất cả 6 test case pass

## 4. Deferred Work (việc đã cố ý hoãn)

| Việc bị hoãn | Hoãn từ story/phase | Lý do hoãn | Mốc quay lại |
|--------------|---------------------|------------|--------------|
| Xây dựng UI/Frontend thực tế | Phase 7 (tất cả story) | Ngoài phạm vi MVP (theo docs/prd.md và docs/architecture.md), chỉ tập trung Backend API Core & Pipeline | Chưa xác định, cần review lại trước khi release |
| Chi tiết deploy produzione (cloud provider, load balancer, tên miền, số instance) | Phase 0 (Story 0.3) | docs/deploy.md chưa được tạo (chờ xác nhận hosting/infra từ người dùng) | Chưa xác định, cần tạo docs/deploy.md trước |
| Cơ chế xác thực client (API key/JWT/không auth) | Phase 1 (Story 1.4, 1.5) | [CẦN LÀM RÕ] trong docs/prd.md Câu 3 | Chưa xác định |
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
