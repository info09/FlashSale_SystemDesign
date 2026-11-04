# Finish Checklist — Danh sách kiểm tra trước khi hoàn thành giai đoạn

File này dùng để tự kiểm tra trước khi coi một giai đoạn implement đã hoàn thành, sẵn sàng commit/tạo PR, hoặc chuyển sang giai đoạn tiếp theo. Mọi mục phải được đánh dấu `[x]` (hoặc ghi lý do nếu không áp dụng) trước khi dichiar hoàn thành.

## 1. Kiểm tra tài liệu (Docs consistency)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 1.1 | `docs/prd.md` có đầy đủ 7 phần theo thứ tự bắt buộc (Context, Đối tượng người dùng, Phạm vi, FR, NFR, Success Metrics, Open Questions) | | |
| 1.2 | Mã FR dùng định dạng FR-XX, không đổi so với bản đầu, có thể trace ngược từ `docs/architecture.md` và `docs/status.md` | | |
| 1.3 | Mã NFR dùng định dạng NFR-XX, không đổi so với bản đầu, có ít nhất: hiệu năng (số liệu cụ thể), bảo mật, khả năng mở rộng, độ tin cậy | | |
| 1.4 | `docs/domain-glossary.md` có mục đích file, bảng thuật ngữ 4 cột, nhóm theo domain, quy tắc bổ sung thuật ngữ mới, ghi chú đa ngôn ngữ | | |
| 1.5 | Mọi thuật ngữ trong `docs/architecture.md`, `docs/ux.md`, `docs/conventions.md` trùng khớp tên chuẩn trong `docs/domain-glossary.md` (không dùng synonyms từ cột "KHÔNG được gọi là") | | |
| 1.6 | `docs/decisions.md` có mục đích file, quy tắc format entry, ngưỡng ghi/không ghi, phần danh sách quyết định (có thể trống nếu chưa có quyết định) | | |
| 1.7 | `docs/status.md` có bảng tiến độ FR, bảng tiến độ NFR, bảng tiến độ theo module, danh sách [CẦN LÀM RÕ], lịch sử thay đổi | | |
| 1.8 | `docs/copilot-instructions.md` có 9 mục ràng buộc (đọc trước khi code, thuật ngữ glossary, trace FR/NFR, conventions, ux, cập nhật status, cập nhật decisions, không đặt tên tạm, kiểm tra trước commit, xử lý [CẦN LÀM RÕ]) | | |

## 2. Kiểm tra Functional Requirements (FR)

| Mã FR | Nội dung ngắn | Đã implement | Đã test | Trace về architecture/module | Link PR/Commit |
|-------|--------------|--------------|---------|------------------------------|----------------|
| FR-01 | Rate Limit qua Redis Lua Script (Token Bucket), trả 429 nếu vượt | | | | |
| FR-02 | Trừ kho atomic trên Redis qua Lua Script, lỗi hết hàng nếu không đủ | | | | |
| FR-03 | Đẩy Message vào RabbitMQ Queue, trả Http202Accepted ngay | | | | |
| FR-04 | Async Worker ghi Order + Outbox trong 1 transaction PostgreSQLPrimary | | | | |
| FR-05 | Retry ExponentialBackoff, sau 3 lần chuyển vào DeadLetterQueue | | | | |
| FR-06 | Endpoint HealthCheck và xuất Metric cho Prometheus/Grafana | | | | |
| FR-07 | Bộ k6 Load Test giả lập 8.000 VirtualUser | | | | |

## 3. Kiểm tra Non-Functional Requirements (NFR)

| Mã NFR | Nội dung ngắn | Đã đạt | Cách đo / bằng chứng | Link PR/Commit |
|--------|--------------|--------|----------------------|----------------|
| NFR-01 | p99 latency FastPath < 20ms | | k6 test result | |
| NFR-02 | Chịu spike 15.000 RPS, không 5xx (ngoại 429) | | k6 test result | |
| NFR-03 | 8.000 VirtualUser, Error Rate < 1% | | k6 test result | |
| NFR-04 | HTTPS bắt buộc; RateLimit phân tán qua Redis | | Kiểm tra config | |
| NFR-05 | Scale horizontally, trạng thái chia sẻ qua Redis | | Kiểm tra triển khai | |
| NFR-06 | Không thất thoát Order 202 Accepted; Data Consistency 100% | | Outbox reconciliation | |
| NFR-07 | Xuất Metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) | | Grafana dashboard | |
| NFR-08 | Tất cả dịch vụ trong Docker, khởi tạo qua DockerCompose | | docker-compose up | |

## 4. Kiểm tra terminology (theo docs/domain-glossary.md)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 4.1 | Không có từ `Gateway`, `Worker`, `Stock`, `Quantity`, `Throttling`, `RateLimiter`, `Dlq`, `Master`, `Slave`, `EventStore`, `AuditLog`, `OutboxTable` trong code | | |
| 4.2 | Không có `orders`, `order_items`, `outbox_table`, `event_store`, `audit_log` làm tên bảng DB | | |
| 4.3 | Không có `order_queue`, `dlq_queue`, `dead_letter_queue`, `failed_messages` làm tên Queue/Exchange | | |
| 4.4 | Không có `HTTP_202`, `DLQ`, `FAILED` làm hằng số/enum | | |
| 4.5 | Không có `FlashSale.Gateway`, `FlashSale.Backend`, `FlashSale.Worker`, `FlashSale.Stock` làm namespace | | |
| 4.6 | Mọi class/module quan trọng có thể trace về FR/NFR trong `docs/prd.md` | | |

## 5. Kiểm tra UX / API copy (theo docs/ux.md)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 5.1 | Response JSON dùng camelCase, message thông báo dùng tiếng Việt, thuật ngữ từ glossary | | |
| 5.2 | Trả về HTTP 202 Accepted khi nhận đơn thành công (không chờ xử lý xong) | | |
| 5.3 | Trả về HTTP 429 khi vượt RateLimit, message rõ ràng | | |
| 5.4 | Trả về lỗi hết hàng khi Inventory không đủ, không tạo Order | | |
| 5.5 | Mọi trạng thái lỗi (hết hàng, vượt rate limit, lỗi hệ thống) được hiển thị rõ ràng | | |
| 5.6 | Có `Correlation-Id` header để tracer request xuyên dịch vụ | | |

## 6. Kiểm tra observability (theo docs/architecture.md, docs/conventions.md)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 6.1 | Endpoint `/health` hoặc `/api/v1/health` trả về Healthy/Unhealthy | | |
| 6.2 | Metric `P99Latency`, `ErrorRate`, `QueueDepth`, `DlqSize`, `InventoryRemaining` đã xuất | | |
| 6.3 | Prometheus có thể scrape metric từ ApiGateway và AsyncWorker | | |
| 6.4 | Grafana Dashboard hiển thị metric trên | | |

## 7. Kiểm tra triển khai (theo docs/architecture.md, NFR-08)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 7.1 | `docker-compose.yml` khởi tạo toàn bộ dịch vụ (ApiGateway, AsyncWorker, Redis, RabbitMQ, PostgreSQL, k6, Prometheus, Grafana) | | |
| 7.2 | Tất cả dịch vụ chạy trong container Docker | | |
| 7.3 | ApiGateway, AsyncWorker, Redis Cluster có thể scale horizontally | | |

## 8. Kiểm tra cập nhật trạng thái (theo docs/status.md, docs/decisions.md)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 8.1 | `docs/status.md` đã được cập nhật trạng thái tất cả FR/NFR đã implement | | |
| 8.2 | `docs/status.md` đã ghi link PR/commit cho mỗi FR/NFR đã Done | | |
| 8.3 | `docs/decisions.md` đã ghi tất cả quyết định kỹ thuật mới (nếu có) | | |
| 8.4 | Không có entry bị xóa trong `docs/decisions.md` (chỉ đổi status thành Superseded) | | |

## 9. Kiểm tra điểm [CẦN LÀM RÕ] (theo docs/prd.md)

| # | Câu hỏi mở | Đã có câu trả lời | Nếu chưa, đã đánh dấu [CẦN LÀM RÕ] |
|---|-----------|-------------------|-----------------------------------|
| 1 | Ngưỡng rate limit cụ thể (per customer/IP) | | |
| 2 | Tên RabbitMQ Exchange/Queue/routing key | | |
| 3 | Cơ chế xác thực client (API key/JWT/không auth) | | |
| 4 | Replication lag acceptable Primary→Replica | | |
| 5 | Thời gian purge/archive Outbox table | | |
| 6 | Thời gian giữ message trong DeadLetterQueue | | |

## 10. Kiểm tra trước khi commit / tạo PR (theo docs/copilot-instructions.md)

| # | Mục kiểm tra | Trạng thái | Ghi chú |
|---|-------------|------------|---------|
| 10.1 | Đã đọc đủ 7 file docs trước khi code | | |
| 10.2 | Mọi tên trong code trùng khớp glossary | | |
| 10.3 | Không có từ CẤM dùng lọt vào code | | |
| 10.4 | Code trace về FR/NFR trong PRD | | |
| 10.5 | `docs/status.md` đã được cập nhật | | |
| 10.6 | Quyết định kỹ thuật mới (nếu có) đã ghi vào `docs/decisions.md` | | |
| 10.7 | Thuật ngữ mới (nếu có) đã bổ sung vào `docs/domain-glossary.md` | | |

## 11. Kết luận

- [ ] Tất cả mục trên đã đánh dấu `[x]` hoặc có lý do không áp dụng.
- [ ] Giai đoạn hiện tại có thể coi là hoàn thành, sẵn sàng commit/tạo PR hoặc chuyển sang giai đoạn tiếp theo.

Người kiểm tra: ____________________  
Ngày: ____________________  
Link PR/Commit: ____________________
