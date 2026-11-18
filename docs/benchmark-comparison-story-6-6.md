# Story 6.6 — Đối chiếu kết quả k6 với Success Metrics (docs/prd.md)

Ngày cập nhật: 2026-11-18  
Phạm vi: đối chiếu theo `docs/prd.md` mục 6 (Success Metrics), trọng tâm **không over-selling** và **không mất đơn hàng**.

## 1. Success Metrics (PRD) và kết quả đối chiếu

| # | Success Metric (PRD) | Kết quả k6/quan sát hiện có | Trạng thái đối chiếu |
|---|---|---|---|
| 1 | p99 latency luồng nhận đơn < 20ms | Story 6.2: chạy `VUS=10, DURATION=30s, SLEEP_SECONDS=0, THRESHOLD_P99_MS=20` → `http_req_duration p(99)=9.67ms` (pass), `expected_response:true` avg 2.42ms, p95 9.18ms; checks 100% trong tập `{202,400,401,429}`. Lần 8000 VU (30s) ghi nhận latency cao hơn (p95 ~1.71s–5.86s tùy lần chạy), phụ thuộc tải/môi trường. | **Đạt trong lần đo 6.2 (10 VU)**; **chưa xác nhận** đạt ở benchmark 8.000 VU/NFR-01 đầy đủ. |
| 2 | Tỷ lệ lỗi tổng thể < 1% khi chạy 8.000 VUs đồng thời | Script hỗ trợ `THRESHOLD_ERROR_RATE`/`THRESHOLD_CHECKS_RATE` (Story 6.4). Các lần chạy 8000 VU trước đó có `http_req_failed` cao do 4xx/429/400 (k6 tính 4xx là failed) nhưng checks trạng thái mong đợi pass; chưa có benchmark 8000 VU với `error_rate < 0.01` (5xx/timeout/bất thường) trên môi trường chuẩn. | **Chưa xác nhận** (cần chạy Story 6.4 với inventory/token/rate limit phù hợp). |
| 3 | Không over-selling: tổng số đơn hàng ghi vào PostgreSQL không vượt số lượng tồn kho ban đầu | Cơ chế: trừ kho atomic Redis Lua (`AtomicInventoryOffloading`) + Worker tạo `Order` từ message; e2e/unit/integration các story trước đã kiểm tra luồng trừ kho/nhận đơn. Chưa thực hiện đối chiếu số lượng sau một benchmark tạo nhiều `202 Accepted`. | **Chưa xác nhận bằng số liệu benchmark** (cần truy vấn đối chiếu sau run có 202). |
| 4 | Không mất đơn hàng: 100% đơn nhận 202 Accepted phải có bản ghi tương ứng trong PostgreSQL Primary (Outbox reconciliation) | Cơ chế: Transactional Outbox (`Order` + `Outbox` trong 1 transaction), reconciliation script/process (Story 4.12). Các lần k6 gần đây có rất ít/không có `202` (chủ yếu 400/401/429), nên không phát sinh tập đơn 202 cần đối chiếu. | **Chưa xác nhận bằng benchmark có 202** (cần chạy với inventory/token hợp lệ và đối chiếu). |
| 5 | Không trả về 5xx do quá tải khi spike 15.000 RPS (chỉ cho phép 429) | Script hỗ trợ `EXECUTOR=constant-arrival-rate`, `RATE=15000`, `THRESHOLD_CHECKS_RATE=1` (Story 6.3). Chưa chạy spike 15k RPS thực tế. | **Chưa xác nhận** (cần chạy Story 6.3). |
| 6 | Grafana Dashboard hiển thị đầy đủ metric từ Prometheus, DevOps quản lý DLQ | Story 5.4/5.7: provisioning + dashboard `FlashSale Overview` (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) verified bằng e2e Grafana API test; cần đảm bảo Prometheus scrape đúng metrics path khi vận hành. | **Đạt (cấu hình/dashboard verified)**; vận hành thực tế cần Prometheus scrape `/api/v1/metrics` theo môi trường. |

## 2. Trọng tâm: Không over-selling và Không mất đơn hàng

### 2.1 Không over-selling (Metric #3)
**Điều kiện cần để đối chiếu:** chạy k6 với `PRODUCT_ID` có `initial_inventory = N` (set qua `POST /api/v1/inventory/add`), tạo ra các response `202 Accepted`.

**Cách kiểm tra:**
1. Ghi nhận tồn kho ban đầu `N` và số request trả `202` từ kết quả k6 (hoặc đếm theo `orderId`/log có correlation).
2. Đếm đơn trong PostgreSQL Primary:
   ```sql
   -- Tổng số đơn cho sản phẩm
   SELECT COUNT(*) AS order_count, COALESCE(SUM(quantity), 0) AS total_quantity
   FROM "Order"
   WHERE "ProductId" = '<product-id>';

   -- (Tùy schema) kiểm tra theo thời gian chạy benchmark
   -- SELECT COUNT(*) FROM "Order" WHERE "ProductId"='<product-id>' AND "CreatedOn" >= '<start>' AND "CreatedOn" <= '<end>';
   ```
3. Đối chiếu: `order_count` (hoặc `total_quantity` theo nghiệp vụ) **<= N** và không vượt số lượng đã trừ thành công. Kiểm tra tồn kho còn lại trong Redis/Inventory API nếu cần.

**Kết luận hiện tại:** chưa có dữ liệu benchmark với `202 Accepted` để đối chiếu định lượng; cần thực hiện sau Story 6.3/6.4 trong môi trường hợp lệ.

### 2.2 Không mất đơn hàng (Metric #4)
**Điều kiện cần:** tập các request nhận `HTTP 202 Accepted` (đơn đã được chấp nhận vào pipeline).

**Cách kiểm tra (Outbox reconciliation):**
1. Thu thập danh sách `orderId`/`CorrelationId` từ response 202 (k6 có thể log/extend script nếu cần thu thập chi tiết).
2. Đối chiếu trong PostgreSQL Primary:
   ```sql
   -- Đơn hàng tồn tại
   SELECT "Id", "ProductId", "Quantity", "OrderStatus", "CreatedOn"
   FROM "Order"
   WHERE "Id" IN ('<order-id-1>', '<order-id-2>', ...);

   -- Bản ghi Outbox tương ứng (Transactional Outbox)
   SELECT "Id", "OrderId", "ProcessingStatus", "OccurredOn"
   FROM "Outbox"
   WHERE "OrderId" IN ('<order-id-1>', ...);
   ```
3. Chạy/đối chiếu theo reconciliation script/process của Story 4.12: **100% đơn 202** phải có bản ghi `Order` (và `Outbox` theo thiết kế) trong PostgreSQL Primary sau khi pipeline xử lý hoàn tất.
4. Kiểm tra DLQ/failed processing: message lỗi sau retry phải vào DLQ (FR-05), không âm thầm mất.

**Kết luận hiện tại:** pipeline Outbox + Worker + reconciliation đã được implement ở Phase 4; chưa có tập `202 Accepted` từ benchmark quy mô để đối chiếu end-to-end 100%.

## 3. Kết luận Story 6.6
- Đã **đối chiếu** Success Metrics PRD với bằng chứng k6 hiện có và cơ chế hệ thống.
- **Xác nhận một phần:** p99 < 20ms đạt trong lần đo 6.2 (10 VU, p99 9.67ms); Grafana dashboard/provisioning verified (5.4/5.7).
- **Chưa xác nhận định lượng:** over-selling = 0 và không mất đơn 202 (cần benchmark tạo 202 + truy vấn PostgreSQL/Outbox reconciliation), error rate < 1% ở 8000 VU, spike 15k RPS không 5xx (6.3/6.4).
- **Khuyến nghị chạy benchmark chuẩn:** set inventory/token/rate limit phù hợp → chạy 6.2/6.3/6.4 → thu thập response 202 → truy vấn `Order`/`Outbox` và so sánh với inventory ban đầu + reconciliation.
