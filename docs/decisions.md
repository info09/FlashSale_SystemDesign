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

### [2026-11-09] — Them consumer downstream OrderNotification xuat phat tu Outbox

- **Boi canh (Context):** Sau Story 4.14, OutboxPublisherWorkerService xuat phat cac ghi Outbox Pending ra Order Notification Exchange (order_notification_exchange, routing key order.notification), nhung chua co consumer downstream nao tieu thu message tu Queue order_notification_queue. Can them consumer de hoan thanh pipeline outbox -> downstream (NFR-06, FR-04).
- **Cac lua chua da xem xuat (Options considered):**
  - Khong them consumer: message vao Queue order_notification_queue vo han, khong co he thong nao tien tiep xu ly su kien Outbox da xuat phat.
  - Dung chung Worker chinh (order_pipeline_queue) de consume: gay nham luan, vi Queue chinh la cho OrderCreated message, khong phai cho thong bao downstream.
  - Them OrderNotificationConsumerService + OrderNotificationWorkerService (BackgroundService) consume tu Queue order_notification_queue: tach roi rang, doc lap tu Worker chinh, hoan thanh pipeline downstream.
- **Quyet dinh (Decision):** Chon lua chua ba: them OrderNotificationConsumerService (xu ly nghiep vu downstream, kiem tra Order ton tai) + OrderNotificationWorkerService (BackgroundService consume qua BasicGet tu Queue order_notification_queue); dang ky AddScoped<OrderNotificationConsumerService> + AddHostedService<OrderNotificationWorkerService> trong Worker/Program.cs.
- **Ly do (Rationale):** Hoan thanh pipeline Outbox (Pending -> Publish ra Queue downstream -> Consumer downstream tien tiep xu ly), dam bao su kien da luu duoc tien tiep xu ly (NFR-06). Consumer downstream co idempotency (kiem tra Order ton tai truoc khi xu ly).
- **Danh doi/he quy (Trade-off):** Can them BackgroundService moi (OrderNotificationWorkerService) chay song song voi Worker chinh, tang tai nguyen it it. Neu can giam latency, co the giam interval poll hoac dung EventingConsumer (BasicConsume) thay vi BasicGet.
- **Trang thai (Status):** Active

### [2026-11-18] — Chọn Angular cho UI/Frontend

- **Bối cảnh (Context):** Phase 7 (UI/Frontend, ngoài phạm vi MVP backend) cần công nghệ frontend để thiết kế/triển khai màn hình theo `docs/ux.md` (ví dụ màn hình Gọi API mua hàng cho FlashSaleShopper). Cần chốt framework để thống nhất triển khai và convention khi bắt đầu code UI.
- **Các lựa chọn đã xem xét (Options considered):**
  - React/Next.js: phổ biến, hệ sinh thái lớn; team cần thống nhất style/state management.
  - Vue/Nuxt: nhẹ, dễ tiếp cận; khác stack quen thuộc của team.
  - Angular: framework đầy đủ (routing, forms, DI, HttpClient), phù hợp ứng dụng enterprise và phong cách .NET/C#; đã được nhắc trong hồ sơ tech stack (`about-me.md`).
- **Quyết định (Decision):** Chọn **Angular** làm framework cho UI/Frontend của dự án.
- **Lý do (Rationale):** Thống nhất hướng triển khai UI Phase 7, tận dụng stack quen thuộc (Angular), hỗ trợ tổ chức component/service/form rõ ràng khi hiện thực các màn hình và luồng trong `docs/ux.md` (FlashSaleShopper, DevOps dashboard tích hợp nếu có).
- **Đánh đổi/hệ quả (Trade-off):** Cần thiết lập workspace/build/frontend tooling (Node, Angular CLI) khi triển khai thực tế; UI vẫn ngoài phạm vi MVP backend hiện tại, các story Phase 7 (thiết kế/implement/test UI) sẽ bám theo quyết định này.
- **Trạng thái (Status):** Active

### [2026-11-18] — Thông tin bảng nhập thông tin khi confirm giữ chỗ (Phase 8)

- **Bối cảnh (Context):** Luồng Hold/Reservation (Phase 8, `docs/plan-hold-reservation-flow.md`) yêu cầu người dùng nhập thông tin sau khi giữ chỗ thành công và submit để confirm mua. Cần chốt các field, validation cơ bản và nơi lưu trữ để thiết kế API/DB/UI.
- **Các lựa chọn đã xem xét (Options considered):**
  - Chỉ forward thông tin trong message/payload, không lưu DB: đơn giản nhưng khó truy vấn/lịch sử đơn và không đáp ứng nhu cầu lưu thông tin khách hàng.
  - Lưu thông tin ở DB (gắn với `Order` hoặc bảng liên quan): phục vụ lưu trữ và xử lý đơn; cần chốt schema/migration khi implement.
  - Tập field tối thiểu khác (email, ghi chú, nhiều địa chỉ): chưa được yêu cầu tại thời điểm chốt.
- **Quyết định (Decision):** Bảng nhập thông tin khi confirm gồm **Họ tên** (`Name`), **Phone** (`PhoneNumber`), **Địa chỉ** (`Address`); **bắt buộc nhập đầy đủ cả 3 trường**. Khi khách hàng submit confirm phải **lưu được thông tin vào DB** trong bảng mới **`InformationCustomer`** với các cột: **`Id`, `Name`, `Address`, `PhoneNumber`, `HoldId`**.
- **Lý do (Rationale):** Đáp ứng yêu cầu nghiệp vụ thu thập và lưu trữ thông tin liên hệ/giao nhận khi xác nhận mua trong luồng giữ chỗ; cấu trúc bảng rõ ràng để triển khai lưu trữ gắn với hold/đơn hàng.
- **Đánh đổi/hệ quả (Trade-off):** Cần bổ sung schema/migration/kiểu dữ liệu/ràng buộc (PK/FK/độ dài/unique nếu cần) cho bảng `InformationCustomer` và mapping liên kết với `Order`/hold khi triển khai; quy tắc validation chi tiết và việc đưa thông tin vào message/payload sẽ được cụ thể hóa ở bước thiết kế/implement.
- **Trạng thái (Status):** Active

### [2026-11-18] — Quy tắc luồng Hold/Reservation (Phase 8)

- **Bối cảnh (Context):** Cần chốt hành vi nghiệp vụ/kỹ thuật của luồng giữ chỗ 1 phút (Phase 8) để triển khai API/Redis/UI/worker và đảm bảo nhường slot đúng khi hết hạn.
- **Các lựa chọn đã xem xét (Options considered):**
  - Giữ chỗ không reserve tồn kho ngay: đơn giản về ngữ nghĩa "chưa trừ" nhưng khó đảm bảo slot không bị người khác lấy trong thời gian hold.
  - Reserve ngay khi hold và trả lại khi hết hạn/hủy, confirm chỉ chốt hold: đảm bảo giữ slot đúng và nhường chỗ khi hết hạn; cần cơ chế release idempotent.
  - Cho phép nhiều hold/user hoặc resume sau reload: linh hoạt hơn nhưng tăng độ phức tạp trạng thái và chống abuse.
  - Thay thế hoàn toàn `POST /api/v1/flash-sale/buy`: đơn giản luồng nhưng breaking; chạy song song an toàn hơn khi chuyển đổi.
  - Realtime/push khi hết hạn: UX tức thời hơn nhưng tăng độ phức tạp; kiểm tra khi submit + countdown phía client đủ cho nhu cầu hiện tại.
  - Nhả slot trong worker hiện có hoặc worker riêng: worker riêng tách trách nhiệm và dễ vận hành/scale độc lập.
- **Quyết định (Decision):** (1) **Reserve ngay khi hold** (giảm available trên Redis để giữ slot), **trả lại khi hết hạn/hủy**, **confirm chỉ chốt hold** (không trừ thêm tồn kho). (2) **1 user chỉ giữ 1 hold cùng lúc**. (3) Endpoint cũ **`POST /api/v1/flash-sale/buy` chạy song song** với hold/confirm. (4) Hết hạn xử lý bằng **kiểm tra khi submit** là đủ (server chặn confirm + nhả slot; UI countdown). (5) **Có nút Hủy giữ chỗ**; **reload trang thì mất hold** (không resume trên UI). (6) Tạo worker riêng **`FlashSale.WorkerUnHold`** quét nhả slot **mỗi 1 giây** bằng `holds:expiring` + Lua `release_if_expired` (idempotent). (7) Anti-abuse: 1 hold/user + RateLimit hiện có cho hold/confirm, chưa thêm giới hạn bổ sung.
- **Lý do (Rationale):** Đảm bảo tính đúng của giữ chỗ và nhường slot trong flash sale (FR-02/FR-03, NFR-06), giới hạn chiếm dụng tài nguyên theo user, giảm rủi ro rollout bằng chạy song song endpoint cũ, giữ độ phức tạp UI/hạ tầng ở mức phù hợp, và tách trách nhiệm nhả slot sang worker riêng.
- **Đánh đổi/hệ quả (Trade-off):** Cần thêm project/service `FlashSale.WorkerUnHold` và cơ chế Redis index/scan; reload/đóng tab không tự trả slot ngay nên phụ thuộc job quét (trễ mục tiêu ~1–2s sau hết hạn); validation chi tiết/mapping DB-message cho `InformationCustomer` vẫn cần cụ thể hóa ở bước thiết kế/implement.
- **Trạng thái (Status):** Superseded (bởi [2026-11-18] — Quy tắc luồng Hold/Reservation theo flow chuẩn, Phase 8)

### [2026-11-18] — Quy tắc luồng Hold/Reservation theo flow chuẩn (Phase 8)

- **Bối cảnh (Context):** Cần chuẩn hóa luồng giữ chỗ 1 phút theo yêu cầu: kiểm tra Redis khi giữ chỗ, hiển thị form và giữ 1 phút, hết hạn/reload thì trả quantity, submit thì xử lý qua Outbox pattern để lưu `InformationCustomer`, publish message và tạo Order.
- **Các lựa chọn đã xem xét (Options considered):**
  - Flow hiện tại (publish message tạo đơn ngay khi confirm, worker ghi Order+Outbox): phù hợp kiến trúc hiện có nhưng khác trình tự xử lý mà Đại ca yêu cầu.
  - Flow chuẩn theo yêu cầu: submit tạo payload Outbox, Outbox pattern chạy để lưu `InformationCustomer` và publish message, RabbitMQ consumer tạo Order — thay đổi trình tự/trách nhiệm xử lý so với hiện tại, cần thiết kế chi tiết transaction/consistency.
- **Quyết định (Decision):** (1) **Bấm giữ chỗ:** kiểm tra Redis đủ quantity; không đủ trả lỗi hết hàng, đủ thì reserve/giữ chỗ 1 phút và hiển thị bảng nhập thông tin. (2) **Không submit trong 1 phút hoặc reload trang:** trả lại quantity trong Redis và nhường chỗ (qua hết hạn/release, `FlashSale.WorkerUnHold` quét mỗi 1s). (3) **Submit:** hệ thống tạo payload trong Outbox pattern; Outbox pattern chạy và lưu `InformationCustomer` (`Id`,`Name`,`Address`,`PhoneNumber`,`HoldId`) + publish message; RabbitMQ consumer nhận message và tạo Order. (4) Giữ các quyết định đã chốt: reserve ngay khi hold, trả lại khi hết hạn/hủy, confirm chốt hold; 1 user/1 hold; `/buy` chạy song song; kiểm tra hết hạn khi submit; có nút Hủy giữ chỗ, reload mất hold trên UI; `FlashSale.WorkerUnHold` quét 1s; 1 hold/user + RateLimit hiện có.
- **Lý do (Rationale):** Thống nhất luồng theo yêu cầu nghiệp vụ của Đại ca, đảm bảo giữ slot/nhường chỗ và lưu thông tin khách hàng thông qua Outbox pattern trước khi tạo Order.
- **Đánh đổi/hệ quả (Trade-off):** Thay đổi đáng kể luồng xử lý so với kiến trúc hiện tại (consumer ghi Order+Outbox từ message đầu vào); cần thiết kế lại transaction/điểm lưu `InformationCustomer`/payload Outbox/publish/consumer tạo Order để đảm bảo nhất quán và idempotency; chi tiết kỹ thuật cụ thể hóa ở Story 8.2.
- **Trạng thái (Status):** Active

### [2026-11-18] — Semantics metric InventoryRemaining trong luồng Hold (Phase 8)

- **Bối cảnh (Context):** Luồng hold reserve tồn kho ngay khi giữ chỗ và trả lại khi hết hạn/hủy, ảnh hưởng cách hiểu chỉ số `InventoryRemaining` trên metrics/dashboard.
- **Các lựa chọn đã xem xét (Options considered):**
  - `InventoryRemaining` = tồn kho vật lý/ban đầu trừ đơn đã chốt: không phản ánh slot đang bị giữ tạm thời.
  - `InventoryRemaining` = tồn kho khả dụng để giữ/chốt (available sau reserve hold): phản ánh đúng khả năng nhận giữ chỗ mới.
  - Thêm metric riêng cho hold/reserved/available: rõ hơn nhưng tăng phạm vi thay đổi observability.
- **Quyết định (Decision):** `InventoryRemaining` phản ánh **tồn kho khả dụng để giữ/chốt** (available sau khi reserve hold; tăng lại khi hết hạn/hủy/nhả slot). Chưa thêm metric hold riêng trong phạm vi chốt hiện tại.
- **Lý do (Rationale):** Phù hợp hành vi nghiệp vụ "giữ chỗ" và giúp DevOps/quan sát thấy số lượng còn có thể cấp slot.
- **Đánh đổi/hệ quả (Trade-off):** Cần cập nhật mô tả/label dashboard hoặc tài liệu metrics ở 8.2/8.8 nếu tên gọi gây hiểu nhầm; nếu cần theo dõi chi tiết reserved/committed có thể bổ sung metric sau.
- **Trạng thái (Status):** Active
