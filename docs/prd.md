# PRD — Distributed Flash-Sale Engine & Event-Driven Order Pipeline

## 1. Context & Problem Statement

Trong các chiến dịch Flash Sale, lưu lượng truy cập tăng đột ngột lên mức spike khoảng 15.000 requests/giây (RPS). Hệ thống bán hàng truyền thống không chịu được áp lực này vì:

- **Database truyền thống bị nghẽn I/O và tràn truy vấn lock dòng (Row Locking):** Hàng ngàn truy vấn ghi đồng thời cố gắng trừ kho cho cùng một sản phẩm. Row lock ở PostgreSQL khiến các truy vấn chờ lẫn nhau, latency đẩy lên khoảng 320ms p99, hệ thống treo.
- **Over-selling (bán vượt tồn kho):** Do Race Condition, nhiều request cùng đọc số lượng tồn kho > 0 trước khi bất kỳ request nào ghi nhận việc trừ kho, dẫn đến bán nhiều hơn số hàng thực có.
- **Mất mát dữ liệu đơn hàng:** Khi dịch vụ xử lý ghi nhận lỗi đột ngột hoặc sập mạng giữa các bước xử lý, đơn hàng đã nhận về phía client nhưng không bao giờ được ghi vào database.

Hậu quả nếu không giải quyết: hệ thống sập toàn bộ trong giờ vàng của sale, khách hàng bị từ chối hoặc nhận đơn thành công rồi sau đó bị hủy vì hết hàng, doanh nghiệp mất doanh thu và uy tín.

Mục tiêu của dự án: xây dựng Backend API Core và pipeline xử lý đơn hàng sự kiện-driven, tự bảo vệ trước traffic lớn, chống over-selling, không mất đơn hàng, đáp ứng p99 latency dưới 20ms trên luồng nhận đơn (fast-path).

## 2. Đối tượng người dùng

| Persona | Mô tả | Hành vi chính | Mức độ am hiểu kỹ thuật | Ảnh hưởng đến UX |
|---------|-------|---------------|------------------------|------------------|
| Khách hàng săn sale (Flash Sale Shopper) | Người dùng thao tác đặt mua cực nhanh ngay thời điểm mở bán. | Bấm nút mua, mong nhận phản hồi tức thì, không chấp nhận hệ thống báo thành công sau đó hủy đơn vì hết hàng. | Không am hiểu kỹ thuật. Chỉ quan tâm kết quả thành công/thất bại và thời gian phản hồi. | Phản hồi < 50ms (mục tiêu p99 < 20ms). Thông báo lỗi rõ ràng ngay khi hết hàng hoặc bị giới hạn tần số requests. Không có giao diện UI trong MVP (chỉ API), vì vậy UX hiện tại là phản hồi HTTP status code và body JSON. |
| Kỹ sư vận hành hệ thống (DevOps / System Admin) | Quản trị và giám sát hệ thống sản xuất. | Cấu hình rate limit, theo dõi metric Prometheus/Grafana, xử lý DLQ, khôi phục dữ liệu khi sự cố. | Rất am hiểu kỹ thuật. | Cần các endpoint health/metric, cơ chế cách ly lỗi (resilience), DLQ rõ ràng để replay message, cấu hình docker-compose để triển khai nhanh. |

## 3. Phạm vi (Scope)

| Trong phạm vi (MVP) | Ngoài phạm vi (giai đoạn sau) |
|---------------------|------------------------------|
| Distributed Rate Limiting trên API Gateway qua Redis Lua Script (Token Bucket). | Giao diện UI/Frontend cho người dùng cuối. |
| Atomic Inventory Offloading: trừ kho tức thời trên Redis RAM qua Lua Script (chống over-selling, latency p99 ~18ms). | Tích hợp cổng thanh toán trực tuyến (VNPay, Momo, Stripe, v.v.). |
| Event-Driven Order Pipeline: đẩy công việc tạo đơn vào RabbitMQ Queue, trả về 202 Accepted ngay cho client. | Hệ thống quản lý sản phẩm, danh mục, khuyến mãi chi tiết (Catalog / Coupon Service). |
| Reliable Async Worker: tiêu thụ queue, ghi đơn hàng vào PostgreSQL Primary bằng Transactional Outbox Pattern. | Tích hợp nhà vận chuyển / Logistic pipeline. |
| Resilience & DLQ: Retry với Exponential Backoff, DLQ cho message lỗi sau 3 lần thử. | |
| Benchmarking & Monitoring: bộ k6 Load Test (giả lập 8.000 VUs) và Grafana Dashboard xuất metric Prometheus. | |

## 4. Functional Requirements (FR)

Mã FR dùng định dạng FR-XX, không thay đổi sau này để docs/architecture.md và docs/status.md có thể trace ngược.

- **FR-01:** Khi client gửi request mua hàng đến API Gateway, hệ thống phải kiểm tra rate limit bằng Redis Lua Script (Token Bucket) trước khi xử lý tiếp; nếu vượt ngưỡng, trả về HTTP 429 ngay lập tức mà không gọi xuống dịch vụ hạ tầng.
- **FR-02:** Khi rate limit cho phép, hệ thống phải trừ kho tức thời trên Redis qua Lua Script atomic; nếu tồn kho không đủ, trả về lỗi hết hàng ngay lập tức mà không tạo đơn.
- **FR-03:** Sau khi trừ kho thành công trên Redis, hệ thống phải đẩy thông tin tạo đơn vào RabbitMQ Queue và trả về HTTP 202 Accepted cho client ngay lập tức (không chờ xử lý hoàn tất).
- **FR-04:** Async Worker phải tiêu thụ message từ RabbitMQ, ghi đơn hàng vào PostgreSQL Primary và ghi sự kiện vào Outbox table trong cùng một transaction database (Transactional Outbox Pattern).
- **FR-05:** Nếu xử lý message thất bại, Worker phải retry với Exponential Backoff; sau 3 lần thử vẫn lỗi, message phải được chuyển vào Dead Letter Queue (DLQ).
- **FR-06:** Hệ thống phải cung cấp endpoint health check và metric Prometheus để Grafana Dashboard theo dõi latency, error rate, queue depth, inventory remaining.
- **FR-07:** Bộ k6 Load Test phải giả lập tối đa 8.000 Concurrent Virtual Users (VUs) để benchmark hệ thống trước khi triển khai sản xuất.

[CẦN LÀM RÕ] Ngưỡng rate limit cụ thể là bao nhiêu request/giây per customer hay per IP (input chỉ nêu tổng thể 15.000 RPS spike, chưa chia nhỏ per client).

[CẦN LÀM RÕ] Tên RabbitMQ Exchange/Queue và routing key cụ thể cho pipeline tạo đơn: **Đã chốt** — Exchange: `flash_sale_exchange`, Queue: `order_pipeline_queue`, Routing Key: `order.created` (xem docs/decisions.md).

Cơ chế xác thực/đăng nhập của client khi gọi API: **ASP.NET Core Identity + Bearer Token (JWT)**. Client đăng nhập qua Identity để nhận Bearer Token, sau đó gửi token trong header `Authorization: Bearer <token>` cho mỗi request. API Gateway xác thực token trước khi kiểm tra RateLimit (FR-01, NFR-04).

## 5. Non-Functional Requirements (NFR)

Mã NFR dùng định dạng NFR-XX, không thay đổi sau này.

- **NFR-01 (Hiệu năng):** p99 latency của luồng nhận đơn (fast-path, từ request đến trả về 202 Accepted) phải dưới 20ms. Mục tiêu thực tế khoảng 18ms đối với atomic inventory offloading trên Redis.
- **NFR-02 (Hiệu năng):** Hệ thống phải chịu spike traffic tối đa 15.000 RPS mà không treo và không trả về lỗi 5xx do quá tải hệ thống (ngoại lỗi 429 rate limit).
- **NFR-03 (Khả năng chịu tải):** Xử lý ổn định 8.000 Concurrent Virtual Users với tỷ lệ lỗi (Error Rate) dưới 1%.
- **NFR-04 (Bảo mật):** Tất cả API endpoint phải chỉ cho phép gọi qua HTTPS; mọi request phải kèm Bearer Token hợp lệ (ASP.NET Core Identity) trong header `Authorization: Bearer <token>` trước khi kiểm tra RateLimit; rate limit phải áp dụng phân tán trên nhiều instance API Gateway (không bị bypass khi scale out). Mức tối thiểu: không cho phép client gửi vô hạn request mà không bị chặn.
- **NFR-05 (Khả năng mở rộng):** API Gateway, Worker, và Redis Cluster phải có thể scale horizontally; trạng thái rate limit và inventory phải được chia sẻ qua Redis (không lưu local memory trên mỗi instance).
- **NFR-06 (Độ tin cậy / Uptime):** Không làm thất thoát đơn hàng đã nhận 202 Accepted. Tính nhất quán dữ liệu (Data Consistency) đạt 100% khi ghi DB bất đồng bộ qua Outbox Pattern.
- **NFR-07 (Khả năng quan sát):** Hệ thống phải xuất metric đủ cho Prometheus scrape và hiển thị trên Grafana Dashboard: request rate, error rate, p99 latency, queue depth, DLQ size, inventory remaining.
- **NFR-08 (Triển khai):** Tất cả dịch vụ (API, Worker, Redis, RabbitMQ, PostgreSQL, k6, Prometheus, Grafana) phải được đóng gói trong Docker và có thể khởi tạo bằng docker-compose.

[CẦN LÀM RÕ] Thời gian acceptable replication lag giữa PostgreSQL Primary và Read Replica (đọc) trước khi bị coi là lỗi hệ thống.

[CẦN LÀM RÕ] Thời gian giữ bản ghi trong Outbox table trước khi purge/archive để tránh bảng lớn dần theo thời gian.

[CẦN LÀM RÕ] Thời gian giữ message trong DLQ trước khi tự động xóa hoặc yêu cầu can thiệp thủ công.

## 6. Success Metrics

Dự án được coi là thành công khi đạt tất cả chỉ số sau trong môi trường kiểm tra tải (k6):

- p99 latency luồng nhận đơn < 20ms.
- Tỷ lệ lỗi tổng thể < 1% khi chạy 8.000 VUs đồng thời.
- Không có trường hợp over-selling: tổng số đơn hàng ghi vào PostgreSQL không bao nhiêu hơn số lượng tồn kho ban đầu của sản phẩm trong flash sale.
- Không mất đơn hàng: 100% đơn nhận 202 Accepted cuối cùng phải có bản ghi tương ứng trong PostgreSQL Primary (kiểm tra qua Outbox reconciliation).
- Hệ thống không trả về 5xx do quá tải khi spike 15.000 RPS (chỉ cho phép 429 từ rate limiter).
- Grafana Dashboard hiển thị đầy đủ metric từ Prometheus và DevOps có thể quản lý DLQ (xem, retry, purge).

## 7. Open Questions

Danh sách các điểm [CẦN LÀM RÕ] đã đánh dấu trong PRD, gom lại tại đây để làm rõ sau:

1. Ngưỡng rate limit cụ thể (request/giây per customer hay per IP) — liên quan FR-01, NFR-04.
2. Tên RabbitMQ Exchange/Queue và routing key cụ thể — liên quan FR-03.
3. Cơ chế xác thực/đăng nhập của client khi gọi API: **ASP.NET Core Identity + Bearer Token (JWT)** — đã chốt, liên quan FR-01, NFR-04.
4. PostgreSQL Primary → Read Replica replication lag acceptable — liên quan NFR-06.
5. Thời gian purge/archive bản ghi Outbox table — liên quan NFR-06.
6. Thời gian giữ message trong DLQ trước khi xóa/can thiệp — liên quan FR-05, NFR-07.
