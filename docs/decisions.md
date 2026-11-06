# Decisions Log — Nhật ký quyết định

## Mục đích file

File này dùng để tra cứu **tại sao lúc đó lại chọn thế** khi có người quay lại dự án sau vài tháng và không nhớ lý do.

- Không dùng để mô tả trạng thái hiện tại (việc của `docs/status.md`).
- Không dùng để mô tả kiến trúc (việc của `docs/architecture.md`).

## Quy tắc khi thêm 1 quyết định mới

Mỗi entry phải đúng format sau:

```
### [Ngày / Số thứ tự] — Tên quyết định ngắn gọn

- **Bối cảnh (Context):** Vấn đề gì cần quyết định, tại sao phải quyết định.
- **Các lựa chọn đã xem xét (Options considered):** Danh sách lựa chọn, kể cả lựa chọn không chọn, và lý do loại từng cái.
- **Quyết định (Decision):** Chọn cái gì, 1 câu rõ ràng.
- **Lý do (Rationale):** Vì sao chọn cái này, gắn với NFR/ràng buộc cụ thể trong `docs/prd.md` hoặc `docs/architecture.md` nếu có.
- **Đánh đổi/hệ quả (Trade-off):** Cái gì bị mất / rủi ro khi chọn hướng này.
- **Trạng thái (Status):** Active | Superseded
```

Quy tắc trạng thái:
- Khi có quyết định mới thay thế, **KHÔNG xóa** entry cũ. Chỉ đổi status của entry cũ thành `Superseded` và ghi link trỏ tới entry mới.
- Entry mới phải có status `Active`.

## Ngưỡng khi nào cần ghi vào đây

**PHẢI ghi vào đây:**
- Chọn công nghệ / thư viện chính (ví dụ: Redis vs Memcached, MassTransit vs Raw RabbitMQ client).
- Thay đổi kiến trúc hệ thống.
- Thay đổi convention đã thống nhất (ví dụ: tên gọi trong `docs/domain-glossary.md`).
- Chọn giải pháp cho 1 vấn đề có nhiều cách làm hợp lý và không có đáp án rõ ràng.
- Bỏ qua 1 best practice vì lý do thực tế (ví dụ: không dùng Outbox vì lý do hiệu năng).

**KHÔNG cần ghi vào đây:**
- Tên biến, tên hàm, format code chi tiết (việc của `docs/conventions.md`).
- Việc chi tiết không ảnh hưởng người khác trong team.
- Những quyết định đã được ghi trong `docs/prd.md` dưới dạng yêu cầu (FR/NFR) mà không cần lựa chọn kỹ thuật.

## Danh sách quyết định

### [2026-11-04] — Chọn Scalar làm API documentation

- **Bối cảnh (Context):** Cần có trang tài liệu API tương tác để DevOps và FlashSaleShopper xem các endpoint (inventory deduct, health, v.v.) và thử gọi trực tiếp. Hiện tại chỉ có endpoint "/" và "/health", không có dokumentation API.
- **Các lựa chọn đã xem xét (Options considered):** 
  - Swagger UI (Swashbuckle.AspNetCore alone): phổ biến nhưng giao diện cũ, bundle size lớn.
  - Scalar (Scalar.AspNetCore): hiện đại, nhẹ, hỗ trợ OpenAPI 3, giao diện sạch, dễ tùy biến.
- **Quyết định (Decision):** Chọn Scalar.AspNetCore kèm Swashbuckle.AspNetCore (để sinh OpenAPI JSON), truy cập tại `/scalar/v1`.
- **Lý do (Rationale):** Giao diện hiện đại hơn Swagger UI, đóng gói nhẹ, phù hợp NFR-07 (khả năng quan sát) và hỗ trợ tra cứu endpoint nhanh cho DevOps.
- **Đánh đổi/hệ quả (Trade-off):** Thêm 2 NuGet package vào FlashSale.Api; Scalar phụ thuộc vào Swagger JSON nên vẫn phải giữ Swashbuckle để sinh `/swagger/v1/swagger.json`.
- **Trạng thái (Status):** Active

### [2026-11-04] — Chọn ASP.NET Core Identity + Bearer Token (JWT) cho xác thực client

- **Bối cảnh (Context):** Cần cơ chế xác thực client khi gọi API (FR-01, NFR-04). Trước đây docs/prd.md đánh dấu đây là điểm `[CẦN LÀM RÕ]` (câu 3). Xác thực là điều kiện tiền đề trước khi kiểm tra RateLimit và xử lý đơn hàng, để tránh client ẩn danh gửi vô hạn request.
- **Các lựa chọn đã xem xét (Options considered):** 
  - API Key: đơn giản nhưng khó quản lý vòng đời, xoá key, quay vòng (rotation), không phù hợp cho FlashSaleShopper cá nhân.
  - Không auth trong MVP: vi phạm NFR-04 (không cho phép client gửi vô hạn request mà không bị chặn), không phân biệt được client để áp dụng RateLimit phân tán.
  - ASP.NET Core Identity + Bearer Token (JWT): quản lý user, password hash, đăng nhập, phát hành token; client gửi `Authorization: Bearer <token>` mỗi request.
- **Quyết định (Decision):** Chọn ASP.NET Core Identity + Bearer Token (JWT). Client đăng nhập qua Identity để nhận Bearer Token, gửi trong header `Authorization: Bearer <token>` cho mỗi request. API Gateway xác thực token trước khi kiểm tra RateLimit.
- **Lý do (Rationale):** Đáp ứng NFR-04 (bảo mật, HTTPS bắt buộc + xác thực), cho phép phân biệt client để áp dụng RateLimit phân tán qua Redis (không bị bypass khi scale out). Identity cung cấp sẵn user store, password hashing, login flow.
- **Đánh đổi/hệ quả (Trade-off):** Thêm phụ thuộc `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`; cần bảng Users trong PostgreSQLPrimary và migration mới; tăng độ phức tạp cho luồng đăng nhập trước khi mua hàng.
- **Trạng thái (Status):** Active

### [2026-11-04] — Chọn PascalCase cho tên bảng/cột Database

- **Bối cảnh (Context):** `docs/conventions.md` mục 1.5 ban đầu khuyên dùng snake_case cho tên bảng/cột DB. Sau khi thảo luận, Đại ca yêu cầu đổi sang PascalCase cho tên bảng/cột trong PostgreSQLPrimary.
- **Các lựa chọn đã xem xét (Options considered):** 
  - snake_case (ví dụ: `user`, `user_name`, `role`, `normalized_user_name`): chuẩn ban đầu trong conventions.md, phù hợp với PostgreSQL cộng đồng.
  - PascalCase (ví dụ: `User`, `UserName`, `Role`, `NormalizedUserName`): khớp với tên property C# entity, ít map hơn, dễ đọc hơn cho team .NET.
- **Quyết định (Decision):** Chọn PascalCase cho tên bảng và tên cột trong PostgreSQLPrimary (`User`, `Role`, `UserName`, `Email`, v.v.). Xóa tất cả `HasColumnName(...)` trong `OnModelCreating`, EF Core sẽ dùng tên property làm tên cột mặc định.
- **Lý do (Rationale):** Khớp với tên property C# entity (PascalCase), giảm thursday mapping, đồng nhất với convention đặt tên class/entity trong dự án.
- **Đánh đổi/hệ quả (Trade-off):** Phải sửa `docs/conventions.md` mục 1.5 (ví dụ dùng snake_case) nếu muốn đồng nhất hoàn toàn; tên bảng PostgreSQL viết hoa chữ cái đầu (PostgreSQL lưu tên bảng case-insensitive nếu không bao nháy, nhưng EF Core sẽ quote khi cần).
- **Trạng thái (Status):** Active

### [2026-11-05] — Chọn RabbitMQ.Client trực tiếp thay MassTransit cho Worker consumer

- **Bối cảnh (Context):** Convention 3.13 khuyên dùng MassTransit cho RabbitMQ consumer/producer. Nhưng đã implement `RabbitMqPublisherService` (story 3.3) và `OrderCreatedConsumerService` (story 4.5) dùng `RabbitMQ.Client` trực tiếp, đơn giản, ít phụ thuộc NuGet, phù hợp MVP.
- **Các lựa chọn đã xem xét (Options considered):**
  - MassTransit + `IConsumer<T>`: phức tạp hơn, cần thêm package `MassTransit`, `MassTransit.RabbitMQ`, cấu hình bus, host, endpoint.
  - `RabbitMQ.Client` trực tiếp (`BasicGet`/`BasicPublish`): đơn giản, kiểm soát rõ luồng ack/nack, ít phụ thuộc.
- **Quyết định (Decision):** Chọn `RabbitMQ.Client` trực tiếp cho cả publisher và consumer trong Phase 4 MVP.
- **Lý do (Rationale):** Giảm độ phức tạp, ít package, triển khai nhanh, đủ đáp ứng FR-03, FR-04, FR-05, NFR-06 cho MVP. Có thể chuyển sang MassTransit sau khi scale.
- **Đánh đổi/hệ quả (Trade-off):** Không có tính năng tự động retry/reconnect cao cấp của MassTransit, phải tự xử lý ack/nack và reconnect trong Worker loop. Có thể vi phạm convention 3.13 nếu không ghi quyết định này.
- **Trạng thái (Status):** Active

### [2026-11-04] — Chọn tên RabbitMQ Exchange/Queue/Routing Key cho OrderPipeline

- **Bối cảnh (Context):** Cần tên Exchange/Queue/Routing Key cụ thể cho pipeline tạo đơn (FR-03). Trước đây docs/prd.md đánh dấu đây là điểm [CẦN LÀM RÕ] (câu 2).
- **Các lựa chọn đã xem xét (Options considered):** 
  - Tên tùy chỉnh khác (ví dụ: `flash_sale_order_queue`, `flash_sale_order_exchange`)
  - Tên theo chuẩn đã chốt (ví dụ: `flash_sale_exchange`, `order_pipeline_queue`, `order.created`)
- **Quyết định (Decision):** Chọn tên sau:
  - Exchange: `flash_sale_exchange`
  - Queue: `order_pipeline_queue`
  - Routing Key: `order.created`
  - Dead Letter Queue: `order_pipeline_dlq`
  - Dead Letter Exchange: `flash_sale_dlq_exchange`
  - Dead Letter Routing Key: `order.dlq`
- **Lý do (Rationale):** Tên phù hợp với thuật ngữ chuẩn trong docs/domain-glossary.md (RabbitMQ, Queue, OrderPipeline, Message), dễ hiểu và nhất quán với quy ước đặt tên của dự án.
- **Đánh đổi/hệ quả (Trade-off):** Không có đánh đổi đáng kể, đây là quy ước đặt tên nội bộ.
- **Trạng thái (Status):** Active

### [2026-11-06] — Xác định thời gian purge/archive bảng Outbox (OutboxPurgeDays)

- **Bối cảnh (Context):** Bảng Outbox trong PostgreSQLPrimary dần lớn theo thời gian nếu không có cơ chế purge (liên quan NFR-06, Story 4.13). Cần xác định thời gian lưu trữ trước khi purge/archive để tránh bảng lớn dần.
- **Các lựa chọn đã xem xét (Options considered):**
  - Không purge: bảng Outbox vo hạn lớn, ảnh hưởng đến hiệu năng và chi phí lưu trữ.
  - Purge tất cả Outbox sau PurgeDays (bao gồm chưa/Pending): nguy cơ xóa sự kiện chưa xử lý, làm mất dữ liệu, vi phạm NFR-06.
  - Purge chỉ ghi đã xử lý (Published/Failed) và cũ hơn PurgeDays: an toàn, giữ lại sự kiện chưa xử lý để xử lý lại, đồng hồ NFR-06.
- **Quyết định (Decision):** Chọn lựa chọn ba: purge chỉ các ghi Outbox có ProcessingStatus = Published hoặc Failed và OccurredOn cũ hơn Outbox:PurgeDays (mặc định 30 ngày), trả về số lượng đã xóa. Thông số được đọc từ appsettings/biến môi trường OUTBOX_PURGE_DAYS.
- **Lý do (Rationale):** Đảm bảo không xóa sự kiện chưa xử lý (Pending), đồng hồ với NFR-06 (không mất đơn 202 Accepted), và tránh bảng Outbox lớn dần. Purge thực hiện qua IOutboxRepository.PurgeProcessedOutboxesOlderThanAsync (EF Core ExecuteDeleteAsync) và IOutboxPurgeService orchestrator.
- **Đánh đổi/hệ quả (Trade-off):** Cần một bộ dịch vụ/Worker chạy purge định kỳ (có thể thêm ở story sau). Nếu PurgeDays quá ngắn, có thể xóa sự kiện chưa được DevOps xem/replay DLQ.
- **Trạng thái (Status):** Active

Chờ điền dần trong quá trình implement. Mỗi entry là 1 heading con để dễ tra cứu và link tới.

### [2026-11-06] — Cach chay purge Outbox (tuy chon 3: BackgroundService + API endpoint)

- **Boi canh (Context):** Ham purge Outbox da duoc viet (Story 4.13) nhung chua co mechanism chay tu dong hay kich hoat thu cong. Can xac dinh cach chay de DevOps co the quan ly bang Outbox (NFR-06).
- **Cac lua chua da xem xuat (Options considered):**
  - Chua BackgroundService chay tu dong moi 24 gio: tu dong, khong can can thiep, nhung khong co cach kich hoat ngay khi can.
  - Chua API endpoint goi thu cong: co chinh sua chinh xac, nhung phai nho nguoi goi, de bi bo qua.
  - Ca hai: BackgroundService tu dong moi 24 gio + API endpoint POST /api/v1/outbox/purge (bao ve [Authorize]) de DevOps kich hoat thu cong khi can.
- **Quyet dinh (Decision):** Chon lua chua ba: dang ky OutboxPurgeWorkerService la AddHostedService chay purge moi 24 gio, va them OutboxController co endpoint POST /api/v1/outbox/purge bao ve bang [Authorize] de DevOps kich hoat thu cong.
- **Ly do (Rationale):** Dat dung tu dong (tranh bo qua) va cung co the can thiep thu cong khi can (vi du sau khi xay ra su co, can purge ngay). API endpoint bao ve [Authorize] dam bao chi DevOps co token duoc goi (NFR-04).
- **Danh doi/he quy (Trade-off):** Can luu luong PurgeDays (mac dinh 30 ngay) phu hop; neu qua ngan co the xoa su kien chua duoc xem/replay. BackgroundService chay trong FlashSale.Worker (cung nguyen vien voi PostgreSQLPrimary).
- **Trang thai (Status):** Active

### [2026-11-06] — Chay xuat phat Outbox Pending dinh ky (OutboxPublisherWorkerService)

- **Boi canh (Context):** Sau Story 4.5, OrderCreatedConsumerService chi ghi Order + Outbox (status Pending) vao PostgreSQLPrimary, chua co mechanism xuat phat cac ghi Outbox Pending ra RabbitMQ Queue (OrderPipeline). Can them BackgroundService de dam bao 100% su kien da luu duoc xuat phat (NFR-06, FR-04).
- **Cac lua chua da xem xuat (Options considered):**
  - Khong chay tu dong: Outbox Pending vo han, su kien khong bao gio duoc xuat phat ra Queue, lam mat du lieu, vi pham NFR-06.
  - Chay publish trong OrderCreatedConsumerService cung luc: lam tang phuc tap va gay block luong xu ly message chinh, tac dong den p99 latency (NFR-01).
  - BackgroundService OutboxPublisherWorkerService goi IOutboxPublisher.PublishPendingOutboxAsync dinh ky (moi 30 giay): tach roi rang, khong block luong chinh, dam bao su kien duoc xuat phat sau khoang thoi gian ngan.
- **Quyet dinh (Decision):** Chon lua chua ba: dang ky OutboxPublisherWorkerService la AddHostedService trong FlashSale.Worker, chay publish moi 30 giay, goi IOutboxPublisher.PublishPendingOutboxAsync (lay Outbox Pending, publish ra RabbitMQ, danh dau Published/Failed).
- **Ly do (Rationale):** Tach roi rang cong viec xu ly message chinh (Worker) va cong viec publish Outbox (BackgroundService), khong tac dong den p99 latency cua luong nhan don (NFR-01), van dam bao su kien duoc xuat phat (NFR-06).
- **Danh doi/he quy (Trade-off):** Co khoang tho gian tre it it (toi da 30 giay) giua khi Order duoc ghi va khi Outbox duoc publish ra Queue. Neu can publish ngay lap tuc, co the giam interval hoac goi ngay trong OrderCreatedConsumerService.
- **Trang thai (Status):** Active

