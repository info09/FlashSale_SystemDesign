# Domain Glossary — Ngôn ngữ chung (Ubiquitous Language)

## Mục đích file

Đây là nơi chốt **DUY NHẤT 1 tên gọi** cho mỗi khái niệm nghiệp vụ của dự án.

- Không phải nơi giải thích kiến trúc (việc của `docs/architecture.md`).
- Không phải nơi mô tả UI (việc của `docs/ux.md`).
- Mọi file sinh sau (`architecture.md`, `ux.md`, `conventions.md`, `status.md`, v.v.) và mọi đoạn code / UI copy sau này **PHẢI** dùng đúng tên gọi đã chốt ở đây, không được tự ý dùng từ đồng nghĩa khác.

## Bảng thuật ngữ

### Domain: Traffic Protection (Rate Limiting)

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| API Gateway / `ApiGateway` | Tầng bảo vệ API đầu tiên nhận request từ client, thực hiện rate limit trước khi xuống dịch vụ hạ tầng. | gateway, api, backend, gateway server, entry point | Rate Limit, Client |
| Rate Limit / `RateLimit` | Cơ chế giới hạn số request/giây được phép của mỗi client, áp dụng phân tán qua Redis. | throttling, quota, rate-limiting, rate_limiter, giới hạn tần số, giới hạn requests | API Gateway, Token Bucket, Redis |
| Token Bucket / `TokenBucket` | Thuật toán Laravel/Redis Lua Script dùng để triển khai Rate Limit (đổ token theo thời gian, tiêu token khi có request). | token_bucket, thùng token, thuật toán thùng | Rate Limit, Lua Script |
| Lua Script / `LuaScript` | Script Lua chạy trên Redis để thực thi thao tác nguyên tử (atomic) cho Rate Limit và Inventory Offloading. | lua, script lua, redis script | Redis, Rate Limit, Inventory |
| Client / `Client` | Bản thể gửi request đến API Gateway (trong ngữ cảnh này là Flash Sale Shopper gọi API). | người dùng, user, khách hàng, customer, caller, end user | API Gateway, FlashSaleShopper |

### Domain: Inventory (Kho hàng)

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| Inventory / `Inventory` | Số lượng tồn kho của sản phẩm lưu trên Redis RAM, được trừ atomic qua Lua Script. | stock, tồn kho, kho hàng, quantity, exist_quantity, available_quantity | Product, AtomicInventoryOffloading, OverSelling |
| Atomic Inventory Offloading / `AtomicInventoryOffloading` | Quy trình trừ kho tức thời trên Redis qua Lua Script để chống over-selling, latency p99 ~18ms. | atomic_inventory_offloading, trừ kho, offload kho, giảm kho atomic | Inventory, Lua Script, OverSelling |
| Over-selling / `OverSelling` | Tình trạng bán vượt quá số lượng tồn kho thực do Race Condition khi nhiều request đọc tồn kho >0 cùng lúc trước khi bất kỳ request nào trừ kho. | overselling, bán vượt tồn kho, vượt kho, over_sell, bán trượt | Inventory, RaceCondition, AtomicInventoryOffloading |

### Domain: Order (Đơn hàng) & Pipeline

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| Flash Sale / `FlashSale` | Chiến dịch bán flash gây spike traffic (15.000 RPS), sản phẩm có tồn kho giới hạn. | flash sale, chiến dịch bán flash, giảm giá flash, sale, km | Product, Spike, Order |
| Order / `Order` | Đơn hàng được tạo sau khi trừ kho thành công trên Redis, ghi vào PostgreSQL Primary qua Async Worker. | đơn, đơn hàng, order_item, purchase, transaction, sale, hoa don | FlashSale, Inventory, AsyncWorker, Outbox |
| Fast-path / `FastPath` | Luồng nhận đơn từ request đến trả về HTTP 202 Accepted, mục tiêu p99 latency <20ms. | luồng nhanh, fast path, critical path, đường dẫn nhanh | Order, 202 Accepted, P99Latency |
| Event-Driven Order Pipeline / `OrderPipeline` | Pipeline đẩy công việc tạo đơn vào RabbitMQ Queue và trả về 202 Accepted ngay cho client. | order pipeline, pipeline đơn hàng, sự kiện-driven pipeline | Order, RabbitMQ, Queue, 202 Accepted |
| 202 Accepted / `Http202Accepted` | Mã HTTP trả về ngay cho client sau khi đẩy đơn vào RabbitMQ, không chờ xử lý hoàn tất. | 202, accepted, async accepted, http_202 | OrderPipeline, AsyncWorker, Fast-path |

### Domain: Messaging (RabbitMQ)

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| RabbitMQ / `RabbitMQ` | Message broker dùng để truyền tin giữa API và Async Worker. | rabbit, rabbit_mq, message broker, broker | Queue, Message, AsyncWorker |
| Queue / `Queue` | Hàng đợi RabbitMQ chứa công việc tạo đơn chờ Async Worker tiêu thụ. | queue, hàng đợi, antrian, message queue | RabbitMQ, Message, QueueDepth |
| Message / `Message` | Tin nhắn chứa thông tin tạo đơn được đưa vào Queue và tiêu thụ bởi Async Worker. | message, tin nhắn, sự kiện, event, payload | Queue, AsyncWorker, DeadLetterQueue |

### Domain: Worker & Resilience

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| Async Worker / `AsyncWorker` | Worker tiêu thụ message từ RabbitMQ, ghi đơn hàng vào PostgreSQL Primary và ghi sự kiện vào Outbox trong cùng một transaction. | worker, consumer, processor, background job, async_worker, người tiêu thụ | Queue, Order, Outbox, Retry |
| Retry / `Retry` | Cơ chế thử lại xử lý message khi thất bại, dùng Exponential Backoff. | retry, thử lại, re-attempt, retry_policy, thử lại message | ExponentialBackoff, AsyncWorker, DeadLetterQueue |
| Exponential Backoff / `ExponentialBackoff` | Chiến thuật tăng thời gian chờ theo cấp số nhân giữa các lần Retry. | exponential_backoff, backoff, lùi lại tùy theo cấp số nhân, thời gian chờ tăng dần | Retry, AsyncWorker |
| Dead Letter Queue / `DeadLetterQueue` | Hàng đợi chứa message vẫn lỗi sau 3 lần Retry, để DevOps xem/retry/purge. | dead_letter_queue, dlq, dead letter, error queue, failed queue, hàng đợi thư chết | Retry, AsyncWorker, Queue |
| Resilience / `Resilience` | Khả năng tự bảo vệ và cách ly lỗi của hệ thống trước traffic lớn hoặc sự cố. | resilience, cách ly lỗi, khôi phục, phục hồi, chịu lỗi | DeadLetterQueue, Retry, ApiGateway |

### Domain: Database (PostgreSQL)

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| PostgreSQL Primary / `PostgreSQLPrimary` | Database chính dùng ghi đơn hàng, đối với luồng ghi của Async Worker. | master, primary db, write db, main db, postgresql_primary | ReadReplica, Outbox, Order |
| Read Replica / `ReadReplica` | Database chỉ đọc, tách biệt từ Primary, dùng cho truy vấn đọc. | slave, replica, read db, secondary, read_replica | PostgreSQLPrimary |
| Outbox / `Outbox` | Bảng Outbox trong PostgreSQL Primary lưu sự kiện liên quan đến đơn hàng, dùng trong Transactional Outbox Pattern. | outbox_table, event_store, audit_log, message_outbox, bảng 밖 | PostgreSQLPrimary, Order, DataConsistency |
| Data Consistency / `DataConsistency` | Tính nhất quán dữ liệu: 100% đơn hàng nhận 202 Accepted phải có bản ghi tương ứng trong PostgreSQL Primary. | data_consistency, tính nhất quán dữ liệu, consistency, nhất quán | Outbox, PostgreSQLPrimary, Reconciliation |
| Reconciliation / `Reconciliation` | Quá trình đối chiếu để xác nhận 100% đơn 202 Accepted đã có bản ghi trong PostgreSQL Primary. | reconciliation, khớp dữ liệu, đối chiếu, kiểm tra lại | DataConsistency, Outbox |

### Domain: Observability (Monitoring)

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| Prometheus / `Prometheus` | Hệ thống thu thập và lưu metric từ các dịch vụ. | prom, monitoring server, metrics server, prometheus_server | Grafana, Metric, HealthCheck |
| Grafana / `Grafana` | Dashboard hiển thị metric từ Prometheus để DevOps giám sát. | grafana, dashboard, viz, biểu đồ, bảng quản trị | Prometheus, Metric, HealthCheck |
| Metric / `Metric` | Chỉ số hiệu năng được xuất ra để Prometheus scrape (request rate, error rate, p99 latency, queue depth, dlq size, inventory remaining). | metrics, chỉ số, indicator, kpi, số liệu | Prometheus, Grafana, P99Latency, ErrorRate |
| Health Check / `HealthCheck` | Endpoint cho DevOps kiểm tra trạng thái sống/sẵn sàng của dịch vụ. | health, ping, status, healthcheck, kiểm tra sức khỏe | Prometheus, Grafana |
| P99 Latency / `P99Latency` | Độ trễ tại phần trăm thứ 99 của luồng nhận đơn (fast-path), mục tiêu <20ms. | p99, latency p99, percentile 99, độ trễ p99, p99_latency | Fast-path, Metric, ErrorRate |
| Error Rate / `ErrorRate` | Tỷ lệ lỗi tổng thể của hệ thống, mục tiêu dưới 1% khi 8.000 VUs. | error_rate, tỷ lệ lỗi, lỗi率, error percentage | P99Latency, Metric |
| Queue Depth / `QueueDepth` | Số lượng message đang chờ trong RabbitMQ Queue. | queue_depth, số lượng message trong hàng đợi, antrian depth | Queue, Metric |
| DLQ Size / `DlqSize` | Số lượng message đang chứa trong Dead Letter Queue. | dlq_size, kích thước dlq, số message dlq | DeadLetterQueue, Metric |
| Inventory Remaining / `InventoryRemaining` | Số lượng tồn kho còn lại trên Redis. | inventory_remaining, số lượng tồn kho còn lại, remaining_stock, kho còn | Inventory, Metric |
| SLA / `SLA` | Cam kết mức dịch vụ: p99 latency luồng nhận đơn <20ms. | sla, service_level_agreement, cam kết chất lượng dịch vụ | P99Latency, Uptime |
| Uptime / `Uptime` | Thời gian hệ thống hoạt động liên tục mà không ngừng. | uptime, thời gian hoạt động, sẵn sàng, availability | SLA |

### Domain: Người dùng (Persona)

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| Khách hàng săn sale / `FlashSaleShopper` | Người dùng thao tác đặt mua cực nhanh ngay thời điểm mở bán, yêu cầu phản hồi tức thì (<50ms), không chấp nhận báo thành công sau đó hủy do hết hàng. | người dùng, user, client, khách hàng, customer, end user, shopper | Client, Fast-path, Order |
| Kỹ sư vận hành hệ thống / `DevOps` | Quản trị và giám sát hệ thống sản xuất: cấu hình rate limit, theo dõi Prometheus/Grafana, xử lý DLQ, khôi phục dữ liệu sự cố. | admin, operator, system admin, devops engineer, người vận hành, quản trị viên | Grafana, Prometheus, DeadLetterQueue, HealthCheck |

### Domain: Infrastructure & Testing

| Thuật ngữ chuẩn | Định nghĩa | KHÔNG được gọi là | Thuật ngữ liên quan |
|-----------------|------------|-------------------|---------------------|
| Docker / `Docker` | Công nghệ đóng gói dịch vụ thành container. | docker, container, docker_engine | DockerCompose |
| Docker Compose / `DockerCompose` | Công cụ khởi tạo toàn bộ dịch vụ (API, Worker, Redis, RabbitMQ, PostgreSQL, k6, Prometheus, Grafana) bằng file docker-compose. | docker compose, compose, docker-compose.yml | Docker |
| k6 / `k6` | Công cụ thực hiện benchmarking/load test, giả lập tối đa 8.000 VUs. | k6_load_test, load_test, bộ k6, k6 test | VirtualUser, Benchmarking |
| Virtual User / `VirtualUser` | Người dùng ảo đồng thời trong k6 load test (8.000 VUs). | vu, virtual_user, người dùng ảo, concurrent user, concurrent_virtual_user | k6, Benchmarking |
| Spike / `Spike` | Tăng đột ngột lưu lượng lên 15.000 RPS trong flash sale. | spike, traffic spike, tăng đột ngột, lưu lượng nổ | FlashSale, RateLimit |
| Benchmarking / `Benchmarking` | Quá trình kiểm tra tải bằng k6 trước khi triển khai sản xuất. | benchmark, đánh giá hiệu năng, performance_test, kiểm tra tải | k6, VirtualUser |
| Product / `Product` | Sản phẩm được bán trong flash sale, có tồn kho Inventory được theo dõi. | sản phẩm, product_item, item, goods, hàng hóa | Inventory, FlashSale |

## Ghi chú về đồng âm khác nghĩa giữa domain

Sau khi rà toàn bộ `docs/prd.md`, **chưa phát hiện trường hợp** cùng 1 từ mang nghĩa khác nhau ở 2 domain khác nhau. Nếu sau này phát hiện, phải tách thành 2 dòng riêng (mỗi domain 1 dòng) và nêu rõ distinction, không được gộp chung 1 dòng.

## Quy tắc bổ sung thuật ngữ mới

Khi implement phát sinh khái niệm nghiệp vụ mới chưa có trong bảng trên, **PHẢI** thêm vào đây (đúng format 4 cột) **TRƯỚC KHI** dùng tên đó trong code hoặc UI, không được tự đặt tên tạm rồi dùng luôn. Nếu có nhiều cách gọi hợp lý và cần chọn 1, ghi quyết định chọn tên vào `docs/decisions.md`.

## Ghi chú về đa ngôn ngữ

UI hiển thị tiếng Việt, code viết tiếng Anh. Thuật ngữ chuẩn trong bảng luôn đi kèm cả 2 dạng (tiếng Việt hiển thị / tên tiếng Anh dùng trong code), đây là bảng map **DUY NHẤT** giữa 2 ngôn ngữ. Không được tự dịch khác đi ở bất kỳ đâu khác trong dự án.
