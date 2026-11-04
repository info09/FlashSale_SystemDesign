# Deploy Guide — Distributed Flash-Sale Engine

File này là hướng dẫn vận hành để agent tự thực hiện hoặc hỗ trợ deploy đúng quy trình, không phải tài liệu tham khảo chung chung. Mọi bước deploy phải đáp ứng đúng NFR đã cam kết trong `docs/prd.md` (NFR-04 HTTPS, NFR-06 Data Consistency, NFR-07 Monitoring, NFR-08 Docker) và đúng kiến trúc trong `docs/architecture.md`.

Tên thành phần, entity, bảng, queue, metric PHẢI dùng đúng thuật ngữ trong `docs/domain-glossary.md`.

## 1. Môi trường (Environments)

| Môi trường | Mục đích | Config | Database | Domain | Người dùng chính |
|------------|----------|--------|----------|--------|------------------|
| local | Developer chạy thử trên máy cá nhân | `appsettings.Development.json` | PostgreSQLPrimary local (Docker), ReadReplica tắt | `localhost` | Developer |
| dev | Kích hợp liên tục, chạy test tự động | `appsettings.Development.json` hoặc `appsettings.Dev.json` | PostgreSQLPrimary (Docker Compose dev), ReadReplica tắt | `api-dev.flashsale.local` (hoặc localhost) | DevOps, QA |
| staging | Kiểm tra trước production, gần giống production nhất | `appsettings.Staging.json` | PostgreSQLPrimary (Docker Compose staging), ReadReplica bật | `api-staging.flashsale.com` ([CẦN XÁC NHẬN] tên miền) | DevOps, QA, Product Owner |
| production | Môi trường thực tế phục vụ FlashSaleShopper | `appsettings.Production.json` | PostgreSQLPrimary (cluster), ReadReplica (cluster) | `api.flashsale.com` ([CẦN XÁC NHẬN] tên miền) | FlashSaleShopper, DevOps |

Khác biệt chính giữa các môi trường:
- Connection string PostgreSQLPrimary, ReadReplica, Redis Cluster, RabbitMQ khác nhau per môi trường.
- RateLimit ngưỡng khác nhau (dev thấp, production theo đúng spike 15.000 RPS).
- SSL/HTTPS: local/dev có thể self-signed, staging/production phải dùng chứng chỉ thực (NFR-04).
- Số instance ApiGateway và AsyncWorker: local 1 instance, staging 1-2 instance, production theo quyết định scaling ([CẦN XÁC NHẬN] số instance production).

[CẦN XÁC NHẬN] Nhà cung cấp cloud / nơi host (AWS, Azure, GCP, on-prem server, hay chỉ Docker Compose trên 1 server?). [CẦN XÁC NHẬN] Số môi trường thực tế cần (local/dev/staging/production hay ít hơn?).

## 2. Hạ tầng

Theo `docs/architecture.md` (NFR-08): tất cả dịch vụ đóng gói trong Docker, khởi tạo bằng DockerCompose.

| Thành phần | Vị trí trong repo | Môi trường triển khai | Lưu ý |
|------------|-------------------|----------------------|-------|
| ApiGateway (.NET 8) | `src/FlashSale.Api/` | local, dev, staging, production | Chạy nhiều instance (horizontal scale) theo NFR-05 |
| AsyncWorker (.NET Background Service) | `src/FlashSale.Worker/` | local, dev, staging, production | Chạy nhiều instance theo NFR-05 |
| Redis Cluster | Docker Compose | local, dev, staging, production | Lưu RateLimit state và Inventory (atomic qua LuaScript) |
| RabbitMQ | Docker Compose | local, dev, staging, production | Chứa Queue (OrderPipeline) và DeadLetterQueue |
| PostgreSQLPrimary | Docker Compose | local, dev, staging, production | Ghi Order + Outbox (Transactional Outbox Pattern) |
| ReadReplica | Docker Compose | staging, production (local tắt) | Chỉ đọc, replication từ Primary |
| Prometheus | Docker Compose | dev, staging, production | Thu thập Metric |
| Grafana | Docker Compose | dev, staging, production | Hiển thị DashboardMetric |
| k6 | `tests/Integration.Tests/` | dev, staging | Load test 8.000 VirtualUser |

Cấu trúc service trong `docker-compose.yml` (NFR-08):
- `flashsale-api` (ApiGateway)
- `flashsale-worker` (AsyncWorker)
- `flashsale-redis` (Redis Cluster)
- `flashsale-rabbitmq` (RabbitMQ)
- `flashsale-postgres-primary` (PostgreSQLPrimary)
- `flashsale-postgres-replica` (ReadReplica)
- `flashsale-prometheus` (Prometheus)
- `flashsale-grafana` (Grafana)

[CẦN XÁC NHẬN] Load balancer / reverse proxy trước ApiGateway (nginx, traefik, cloud load balancer?) để terminate TLS theo NFR-04 HTTPS. [CẦN XÁC NHẬN] Số instance ApiGateway và AsyncWorker trong production.

## 3. Quản lý biến môi trường / secret

Quy tắc tuyệt đối: **KHÔNG bao giờ commit secret vào repo** (`.env`, `appsettings.Production.json` chứa giá trị thật, file `.env` thực tế).

| Nơi lưu | Mô tả |
|---------|-------|
| `.env.example` | Template biến môi trường, commit vào repo, KHÔNG chứa giá trị thật |
| `.env` | File thực tế, chứa giá trị thật, **không commit vào repo**, mỗi môi trường 1 file |
| Docker Secret / Cloud Secret Manager | Production: lưu connection string, API key, mật khẩu trong Docker Secret hoặc cloud secret manager (AWS Secrets Manager, Azure Key Vault, v.v.) |
| Environment variable | Container đọc từ `.env` hoặc secret manager khi khởi động |

Danh sách biến bắt buộc phải có để chạy được dự án:

| Biến môi trường | Mô tả | Ví dụ |
|-----------------|-------|-------|
| `POSTGRESQL_PRIMARY_CONNECTION_STRING` | Chuỗi kết nối PostgreSQLPrimary (ghi Order + Outbox) | `Host=...;Database=flashsale;Username=...;Password=...` |
| `POSTGRESQL_REPLICA_CONNECTION_STRING` | Chuỗi kết nối ReadReplica (chỉ đọc) | `Host=...;Database=flashsale_replica;...` |
| `REDIS_CONNECTION_STRING` | Chuỗi kết nối Redis Cluster | `redis://...:6379` |
| `RABBITMQ_CONNECTION_STRING` | Chuỗi kết nối RabbitMQ | `amqp://guest:guest@localhost:5672/` |
| `RABBITMQ_QUEUE_NAME` | Tên Queue OrderPipeline | `order_pipeline_queue` |
| `RABBITMQ_EXCHANGE_NAME` | Tên Exchange | `flash_sale_exchange` |
| `RABBITMQ_ROUTING_KEY` | Routing key | `order.created` |
| `ASPNETCORE_ENVIRONMENT` | Môi trường chạy | `Development`, `Staging`, `Production` |
| `ASPNETCORE_URLS` | URL ApiGateway lắng nghe | `http://+:8080` |
| `RATE_LIMIT_RPS` | Ngưỡng RateLimit per client | `100` (dev), `15000` (production, [CẦN XÁC NHẬN]) |
| `CORRELATION_ID_HEADER` | Tên header correlation | `Correlation-Id` |

Cách agent lấy giá trị khi chạy local:
1. Copy `.env.example` thành `.env`.
2. Điền giá trị thật cho local (có sẵn trong Docker Compose local).
3. Chạy `docker-compose up` hoặc `dotnet run` với environment variable từ `.env`.

## 4. Quy trình build & CI/CD

Pipeline bắt buộc theo thứ tự. Nếu bất kỳ tầng nào fail, pipeline phải dừng, KHÔNG được deploy tiếp.

```
Stage 1: Build
  └─ dotnet build FlashSale.Api / FlashSale.Worker (Release configuration)

Stage 2: Unit Test
  └─ dotnet test tests/FlashSale.Domain.Tests -c Release
  └─ dotnet test tests/FlashSale.Application.Tests -c Release
  └─ Coverage tối thiểu 80% (theo docs/conventions.md mục 5.5)

Stage 3: Integration Test
  └─ Khởi động Docker Compose (Testcontainers hoặc docker-compose.dev.yml)
  └─ dotnet test tests/FlashSale.Infrastructure.Tests -c Release
  └─ dotnet test tests/FlashSale.Api.Tests -c Release
  └─ dotnet test tests/FlashSale.Worker.Tests -c Release
  └─ Coverage tối thiểu 70%

Stage 4: E2E / Smoke Test (staging)
  └─ Deploy lên staging
  └─ Chạy k6 smoke test (số VU nhỏ, xác nhận luồng cơ bản hoạt động)
  └─ Kiểm tra HealthCheck endpoint (/health) trả về Healthy

Stage 5: Deploy production (chỉ khi có xác nhận thủ công từ người dùng)
  └─ Xem mục 6 và mục 9
```

Công cụ CI/CD: [CẦN XÁC NHẬN] GitHub Actions, GitLab CI, Azure DevOps, hay deploy thủ công qua Docker Compose?

Gate bắt buộc: Nếu unit test fail → dừng, không chạy integration. Nếu integration fail → dừng, không deploy staging. Nếu e2e/smoke fail → dừng, không deploy production.

## 5. Database migration

Quy tắc: Migration phải reversible (có thể rollback) hoặc có kế hoạch rollback rõ ràng trước khi áp dụng lên production.

Cách chạy migration ở từng môi trường:

| Môi trường | Lệnh chạy migration | Khi nào chạy |
|------------|---------------------|--------------|
| local | `dotnet ef database update --project src/FlashSale.Api` | Mỗi khi có migration mới |
| dev | `dotnet ef database update --project src/FlashSale.Api --environment Development` | Sau mỗi merge vào nhánh dev |
| staging | Chạy migration script SQL hoặc `dotnet ef database update` với `appsettings.Staging.json` | Trước khi deploy staging |
| production | Chạy migration script SQL đã review, có file rollback kèm theo | Trong maintenance window, sau khi backup DB |

Quy tắc migration:
- Mỗi migration phải có file `{Timestamp}_{MucDich}.cs` theo Convention 1.1.
- Khi tạo bảng mới (Order, Outbox, Product...), phải có migration tương ứng.
- Khi thay đổi cấu trúc bảng, phải có migration mới, KHÔNG được sửa migration cũ đã push.
- Migration phải reversible: nếu thêm cột, phải có logic xóa cột trong `Down()`; nếu tạo bảng, phải có `DropTable` trong `Down()`.
- Trước khi áp dụng migration lên production: backup PostgreSQLPrimary, kiểm tra migration trên staging thành công, chuẩn bị script rollback.
- Không được áp dụng migration trên production khi hệ thống đang trongFlashSale (spike traffic), trừ khi là khẩn cấp và đã có kế hoạch rollback.

[CẦN XÁC NHẬN] Thời gian purge/archive bảng Outbox và thời gian giữ message trong DeadLetterQueue (liên quan NFR-06, FR-05) — cần cấu hình trong migration hoặc job định kỳ.

## 6. Quy trình deploy từng bước

Checklist tuần tự agent phải làm theo đúng thứ tự khi được yêu cầu deploy:

### Bước 1: Pre-check (trước khi build)
- [] Xác nhận môi trường mục tiêu (local/dev/staging/production).
- [] Xác nhận đã có xác nhận từ người dùng nếu là production (xem mục 9).
- [] Kiểm tra `docs/status.md` xem FR/NFR liên quan đã đạt trạng thái `Done` chưa.
- [] Kiểm tra code đã commit hết, không có thay đổi chưa commit.
- [] Kiểm tra `.env` hoặc secret manager đã có giá trị cho môi trường mục tiêu.
- [] Kiểm tra Docker Compose file và Dockerfile đã cập nhật.

### Bước 2: Build
- [] Chạy `dotnet build` (Release) cho `FlashSale.Api` và `FlashSale.Worker`.
- [] Kiểm tra không có lỗi build.

### Bước 3: Chạy test (gate bắt buộc)
- [] Chạy unit test (coverage ≥ 80%).
- [] Chạy integration test (coverage ≥ 70%).
- [] Nếu bất kỳ test nào fail → DỪNG pipeline, không deploy tiếp, báo lỗi cho người dùng.

### Bước 4: Build Docker image
- [] Build `Dockerfile.Api` và `Dockerfile.Worker`.
- [] Tag image theo phiên bản (ví dụ: `flashsale-api:1.0.0`, `flashsale-worker:1.0.0`).
- [] Push image lên container registry (nếu dùng cloud).

### Bước 5: Migration (nếu có thay đổi DB)
- [] Backup PostgreSQLPrimary (nếu là production).
- [] Chạy migration trên môi trường mục tiêu.
- [] Kiểm tra migration thành công (các bảng Order, Outbox, Product đã tồn tại).

### Bước 6: Deploy dịch vụ
- [] Khởi động hoặc cập nhật container qua Docker Compose (hoặc cloud orchestrator).
- [] Xác nhận container `flashsale-api` và `flashsale-worker` đang chạy (không crash loop).
- [] Xác nhận Redis Cluster, RabbitMQ, PostgreSQLPrimary, ReadReplica đã sẵn sàng.

### Bước 7: Smoke test sau deploy
- [] Gọi `GET /health` (HealthCheck) trên ApiGateway, phải trả về `Healthy`.
- [] Gọi `GET /metrics` (nếu có), kiểm tra Prometheus có thể scrape.
- [] Gửi request mua hàng test (FlashSaleShopper) qua API, xác nhận nhận `Http202Accepted`.
- [] Kiểm tra `Order` đã ghi vào `PostgreSQLPrimary` và `Outbox` có bản ghi tương ứng.
- [] Kiểm tra `DeadLetterQueue` không có message lỗi mới từ deploy này.
- [] Kiểm tra Grafana Dashboard hiển thị metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining).

### Bước 8: Hoàn tất
- [] Cập nhật `docs/status.md` tick hạng mục đã deploy.
- [] Thông báo cho người dùng (nếu là production).

## 7. Rollback

### Điều kiện khi nào phải rollback
- HealthCheck trả về `Unhealthy` sau 5 phút deploy.
- Error Rate > 1% (vi phạm NFR-03).
- p99 latency FastPath > 20ms (vi phạm NFR-01).
- Có trường hợp mất đơn hàng (Order nhận 202 Accepted nhưng không ghi vào PostgreSQLPrimary).
- Có lỗi migration làm hỏng dữ liệu PostgreSQLPrimary.
- DeadLetterQueue có số lượng message lỗi tăng đột biến sau deploy.

### Các bước rollback cụ thể

**Rollback local/dev:**
- `docker-compose down` và `docker-compose up` với phiên bản image trước đó.
- Nếu migration đã chạy: chạy migration rollback (`dotnet ef database update --migration {TrướcDo}`).

**Rollback staging:**
- Dừng container mới, khởi động lại container image phiên bản trước.
- Nếu migration đã áp dụng: chạy script rollback SQL đã chuẩn bị.
- Kiểm tra HealthCheck và smoke test lại.

**Rollback production:**
- [CẦN XÁC NHẬN] Thủ tục rollback production (manual, can thiệp DevOps).
- Dừng instance ApiGateway/AsyncWorker mới, quay lại image phiên bản trước.
- Nếu migration đã làm hỏng DB: restore từ backup lấy trước khi deploy.
- Kiểm tra Data Consistency: 100% đơn 202 Accepted phải có bản ghi trong PostgreSQLPrimary (Outbox reconciliation).
- Kiểm tra DeadLetterQueue, reprocess message nếu cần.

Quy tắc rollback: KHÔNG được tự rollback production mà không thông báo cho người dùng trong cùng phiên làm việc. Rollback production luôn cần xác nhận thủ công.

## 8. Monitoring & Logging sau deploy

Cần kiểm tra ngay sau khi deploy để xác nhận hệ thống ổn:

| Kiểm tra | Cách kiểm tra | Ngưỡng chấp nhận | Nếu sai thì làm gì |
|----------|---------------|------------------|-------------------|
| HealthCheck | `GET /health` | Trả về `Healthy` | Rollback theo mục 7 |
| P99Latency | Prometheus / Grafana | < 20ms (NFR-01) | Kiểm tra Redis LuaScript,Database lock |
| ErrorRate | Prometheus / Grafana | < 1% (NFR-03) | Kiểm tra log lỗi, DeadLetterQueue |
| QueueDepth | Prometheus / Grafana | Không tăng liên tục | Kiểm tra AsyncWorker có đang consume không |
| DlqSize | Prometheus / Grafana | 0 hoặc ít sau deploy | Kiểm tra message lỗi, ExponentialBackoff |
| InventoryRemaining | Prometheus / Grafana | Không âm, không sai số | Kiểm tra AtomicInventoryOffloading |
| Log lỗi | Structured log (Serilog/ILogger) | Không có exception mới | Kiểm tra stack trace, correlationId |

Cách kiểm tra:
- Mở Grafana Dashboard, xem các panel metric trên.
- Truy vấn Prometheus: `up{job="flashsale-api"}` phải = 1.
- Xem log container: `docker-compose logs flashsale-api`, `docker-compose logs flashsale-worker`.
- Nếu có lỗi, lấy `CorrelationId` từ log để tracer qua các dịch vụ.

Alert: [CẦN XÁC NHẬN] Có cấu hình alert Prometheus/Grafana không? (ví dụ: gửi thông báo khi ErrorRate > 1% hoặc HealthCheck Unhealthy).

## 9. Quy tắc bắt buộc dành cho agent

- **KHÔNG được tự ý deploy lên production khi chưa có xác nhận rõ ràng từ người dùng trong cùng phiên làm việc, kể cả khi mọi test đã pass.**
- Deploy lên staging có thể tự thực hiện nếu pipeline cho phép (tất cả test pass, pre-check xong).
- Production luôn cần xác nhận thủ công từ người dùng trước khi deploy.
- Trước khi deploy production, agent phải hỏi người dùng: "Bạn có xác nhận deploy lên production không?" và chờ câu trả lời rõ ràng (có/không).
- Sau mỗi deploy thành công hoặc thất bại, phải cập nhật `docs/status.md`.
- Nếu gặp vướng mắc cần người dùng quyết định (cloud provider, load balancer, số instance, tên miền, số instance production, alert config...), phải ghi vào `docs/deploy.md` mục [CẦN XÁC NHẬN] và hỏi người dùng, KHÔNG được tự đoán.

File này được `docs/copilot-instructions.md` bắt buộc đọc trước khi thực hiện bất kỳ hành động liên quan đến deploy/release.
