# Copilot Instructions — Ràng buộc khi implement dự án Flash-Sale Engine

File này là quy tắc bắt buộc cho Copilot (và mọi agent) trước khi viết code, sửa code, hoặc tạo UI cho dự án. Việc không đọc hoặc không tuân thủ file này coi là vi phạm quy ước của dự án.

## 0. Đọc trước khi code (BẮT BUỘC)

Trước khi viết hoặc sửa 1 dòng code, copilot PHẢI đọc đầy đủ các file sau theo thứ tự:

1. `docs/prd.md` — để hiểu FR/NFR, phạm vi (scope), và các điểm [CẦN LÀM RÕ].
2. `docs/domain-glossary.md` — để dùng đúng thuật ngữ chuẩn, không được dùng từ đồng nghĩa.
3. `docs/architecture.md` — để hiểu kiến trúc, module, tech stack, luồng dữ liệu.
4. `docs/ux.md` — nếu làm việc liên quan đến UI, label, copy, component, design tokens.
5. `docs/conventions.md` — để tuân thủ quy ước đặt tên, cấu trúc thư mục, API, database, message broker.
6. `docs/status.md` — để kiểm tra trạng thái FR/NFR trước khi bắt đầu implement.
7. `docs/decisions.md` — để xem các quyết định kỹ thuật đã chốt (nếu có).

Nếu chưa đọc đủ 7 file trên, KHÔNG được bắt đầu viết code.

## 1. Sử dụng đúng thuật ngữ từ docs/domain-glossary.md (BẮT BUỘC)

Mọi tên class, interface, phương thức, biến, bảng dữ liệu, cột, queue, exchange, routing key, metric, endpoint, label UI, thông báo hệ thống PHẢI dùng đúng thuật ngữ chuẩn đã chốt trong `docs/domain-glossary.md` (cột tiếng Anh/code cho code; cột tiếng Việt hiển thị cho UI copy).

**Từ CẤM dùng** (liệt kê ở cột "KHÔNG được gọi là" trong glossary) KHÔNG được xuất hiện trong code, commit message, PR description, hoặc UI copy.

Ví dụ:
- Đúng: `ApiGateway`, `AsyncWorker`, `Inventory`, `Order`, `Outbox`, `RateLimit`, `TokenBucket`, `LuaScript`, `DeadLetterQueue`, `ExponentialBackoff`, `HealthCheck`, `PostgreSQLPrimary`, `ReadReplica`, `OrderPipeline`, `Queue`, `Message`, `Http202Accepted`, `FastPath`, `FlashSale`, `Product`, `Prometheus`, `Grafana`, `P99Latency`, `ErrorRate`, `QueueDepth`, `DlqSize`, `InventoryRemaining`, `DockerCompose`.
- Sai: `Gateway`, `Worker`, `Stock`, `Quantity`, `Throttling`, `RateLimiter`, `Dlq`, `DeadLetter`, `Master`, `Slave`, `EventStore`, `AuditLog`, `OutboxTable`, `Prom`, `Dashboard`, `P99`, `ErrorRate` (viết sai cách), `K6LoadTest`.

Nếu cần dùng 1 khái niệm nghiệp vụ mới chưa có trong glossary: PHẢI bổ sung vào `docs/domain-glossary.md` (đúng format 4 cột) TRƯỚC KHI dùng trong code hoặc UI. Nếu có nhiều cách gọi hợp lý, ghi quyết định chọn tên vào `docs/decisions.md`.

## 2. Trace FR/NFR từ docs/prd.md (BẮT BUỘC)

Mọi class/module/service quan trọng phải có thể trace ngược về ít nhất 1 FR hoặc NFR trong `docs/prd.md` (mã FR-XX / NFR-XX).

- Không được viết code cho tính năng không có trong PRD (tránh scope creep).
- Không được tự suy diễn thêm tính năng ngoài input.
- Nếu thấy thiếu thông tin để implement (ví dụ: ngưỡng rate limit, tên RabbitMQ Queue, cơ chế auth), KHÔNG được tự bịa ra giả định. Phải đánh dấu `[CẦN LÀM RÕ]` trong code comment và ghi vào `docs/prd.md` phần Open Questions (hoặc hỏi người dùng).

## 3. Tuân thủ docs/conventions.md (BẮT BUỘC)

- Cấu trúc thư mục: theo phần 2 của `docs/conventions.md`.
- Quy ước đặt tên: theo phần 3 của `docs/conventions.md` (class, interface, method, variable, namespace, enum).
- Quy ước API: theo phần 4 của `docs/conventions.md` (route, status code, response body, header).
- Quy ước Database: theo phần 5 của `docs/conventions.md` (bảng, cột, Transactional Outbox, Primary/Replica).
- Quy ước Message Broker: theo phần 6 của `docs/conventions.md` (Queue, Exchange, Routing Key, Consumer, Retry, DLQ).
- Quy ước Observability: theo phần 7 của `docs/conventions.md` (Metric, HealthCheck).
- Quy ước xử lý lỗi, logging: theo phần 8, 9 của `docs/conventions.md`.

## 4. Tuân thủ docs/ux.md cho UI / API copy (NẾU LÀM LIÊN QUAN ĐẾN UI)

- Tên màn hình, label, thông báo hệ thống phải dùng đúng thuật ngữ hiển thị (tiếng Việt) từ glossary.
- Error message, thông báo thành công phải theo mẫu trong phần 7 của `docs/ux.md`.
- Component pattern (form, table, modal, toast, empty state, loading skeleton) phải theo phần 5 của `docs/ux.md`.
- Accessibility tối thiểu phải theo phần 6 của `docs/ux.md`.
- Design tokens (màu, typography, spacing, breakpoint) phải theo phần 4 của `docs/ux.md` (đã đánh dấu `[ĐỀ XUẤT - CẦN DUYỆT]`).

Lưu ý: UI/Frontend nằm ngoài phạm vi MVP (theo `docs/prd.md` và `docs/architecture.md`). Nếu chỉ làm API, hãy áp dụng phần 7 (Content/tone of voice) vào JSON response body và HTTP status code.

## 5. Cập nhật docs/status.md sau mỗi thay đổi (BẮT BUỘC)

Khi bắt đầu, đang làm, hoặc hoàn thành 1 FR/NFR:

- Đổi cột Trạng thái trong `docs/status.md` (`Not Started` → `In Progress` → `Done` / `Blocked` / `Deferred`).
- Ghi link PR/commit.
- Ghi chú kết quả test (latency, error rate, v.v. nếu đã có).
- KHÔNG xóa dòng cũ.

## 6. Cập nhật docs/decisions.md khi có quyết định kỹ thuật mới (BẮT BUỘC)

Khi chọn 1 trong nhiều cách làm hợp lý, hoặc thay đổi kiến trúc/convention đã thống nhất:

- Ghi entry mới vào `docs/decisions.md` đúng format 7 trường (Ngày/Số thứ tự, Bối cảnh, Lựa chọn xem xét, Quyết định, Lý do, Đánh đổi, Trạng thái).
- Status của entry mới là `Active`. Nếu thay thế entry cũ, đổi status entry cũ thành `Superseded` và ghi link trỏ tới entry mới.
- KHÔNG xóa entry cũ.

## 7. Không được tự đặt tên tạm

- Không được đặt tên biến, hàm, class, bảng, queue tạm rồi dùng luôn.
- Nếu cần tên mới, phải đi qua quy trình: thêm vào glossary → (nếu cần chọn tên) ghi vào decisions.md → mới dùng trong code.

## 8. Kiểm tra trước khi commit / tạo PR

Trước khi commit hoặc tạo PR, copilot phải tự hỏi:

1. Tôi đã đọc đủ 7 file docs chưa?
2. Mọi tên trong code có trùng khớp glossary không?
3. Có từ CẤM dùng nào lọt vào code không?
4. Code này trace về FR/NFR nào trong PRD không?
5. Tôi đã cập nhật status.md chưa?
6. Nếu có quyết định kỹ thuật mới, tôi đã ghi vào decisions.md chưa?
7. Nếu có thuật ngữ mới, tôi đã bổ sung vào glossary chưa?

Nếu câu trả lời của bất kỳ câu nào là "không", KHÔNG được commit/tạo PR.

## 9. Xử lý điểm [CẦN LÀM RÕ]

Các điểm sau trong `docs/prd.md` vẫn chưa có câu trả lời, copilot KHÔNG được tự bịa ra giá trị:

1. Ngưỡng rate limit cụ thể (per customer hay per IP) — FR-01, NFR-04.
2. Tên RabbitMQ Exchange/Queue và routing key — FR-03.
3. Cơ chế xác thực client (API key, JWT, hoặc không auth) — FR-01, NFR-04.
4. Replication lag acceptable Primary → ReadReplica — NFR-06.
5. Thời gian purge/archive Outbox table — NFR-06.
6. Thời gian giữ message trong DeadLetterQueue — FR-05, NFR-07.

Nếu cần giá trị nào trong 6 điểm này để implement, phải hỏi người dùng trước, không được tự đặt giá trị tạm.
