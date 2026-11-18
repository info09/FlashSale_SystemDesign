# Thiết kế màn hình — Grafana Dashboard (DevOps)

**Story:** 7.2 (Phase 7, theo `docs/ux.md` mục 2, 3 — Luồng B)  
**Phạm vi:** Thiết kế màn hình/luồng cho `DevOps` (Kỹ sư vận hành hệ thống) theo dõi hệ thống qua `Grafana Dashboard`: xem `Metric`, xem `HealthCheck` của `ApiGateway` và `AsyncWorker`, quản lý `DeadLetterQueue` (xem, retry, purge).  
**Hiện trạng triển khai:** đã có dashboard Grafana provisioned `FlashSale Overview` (`uid=flashsale-overview`, datasource Prometheus) hiển thị 5 metric; `HealthCheck` và thao tác quản lý `DeadLetterQueue` hiện chủ yếu qua endpoint/API/ops flow — thiết kế này chuẩn hóa màn hình và hướng mở rộng.

**Thuật ngữ:** dùng đúng `docs/domain-glossary.md` (DevOps, Grafana, Metric, P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining, HealthCheck, ApiGateway, AsyncWorker, DeadLetterQueue, Retry, RateLimit, Inventory).

## 1. Mục tiêu và người dùng

- **Người dùng:** DevOps (am hiểu kỹ thuật, cần quan sát và xử lý sự cố nhanh).
- **Mục tiêu:** nhìn toàn cảnh sức khỏe và hiệu năng hệ thống Flash Sale; phát hiện bất thường (`ErrorRate`, `DlqSize`, `QueueDepth`, `InventoryRemaining`, `P99Latency`); kiểm tra `HealthCheck`; xử lý message lỗi trong `DeadLetterQueue`.
- **FR/NFR liên quan:** FR-05 (Retry/DLQ), FR-06 (HealthCheck, Metric), NFR-07 (quan sát, quản lý DLQ).

## 2. Vị trí trong Information Architecture

```
DevOps (Kỹ sư vận hành hệ thống)
└── Grafana Dashboard
    ├── Xem Metric: P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining (FR-06, NFR-07)
    ├── Xem HealthCheck của ApiGateway và AsyncWorker (FR-06)
    └── Quản lý DeadLetterQueue: xem, retry, purge (FR-05, NFR-07)
```

## 3. Cấu trúc màn hình (Dashboard layout)

### 3.1 Tổng quan bố cục (Desktop ≥ 1024px)

```
+----------------------------------------------------------------------------------+
| Grafana / FlashSale Overview                                  [Time range][↻]   |
+----------------------------------------------------------------------------------+
| Row A — Trạng thái nhanh (Stat/Status)                                           |
| [HealthCheck: ApiGateway] [HealthCheck: AsyncWorker] [DlqSize] [QueueDepth] ...  |
+----------------------------------------------------------------------------------+
| Row B — Hiệu năng & lỗi                                                          |
| [P99Latency (timeseries, by job)]         [ErrorRate (timeseries, by job)]      |
+----------------------------------------------------------------------------------+
| Row C — Hàng đợi & tồn kho                                                       |
| [QueueDepth (timeseries, by job)]         [DlqSize (timeseries, by job)]        |
| [InventoryRemaining (timeseries, by job, full width)]                            |
+----------------------------------------------------------------------------------+
| Row D — Quản lý DeadLetterQueue                                                  |
| [Bảng message DLQ / link tới công cụ quản lý]  [Hành động: Retry | Purge]        |
+----------------------------------------------------------------------------------+
```

- **Tablet (768–1023px):** 2 cột thu gọn, panel xếp dọc; bảng DLQ cuộn ngang.
- **Mobile (< 768px):** 1 cột, stat cards xếp dọc, timeseries full width, bảng DLQ dạng danh sách/card hoặc cuộn ngang; nút hành động tối thiểu 44x44px.

### 3.2 Khu vực và panel

| Khu vực | Mục đích | Thành phần/Panel | Nguồn dữ liệu / liên kết |
|---|---|---|---|
| Header | Ngữ cảnh dashboard | Tên dashboard `FlashSale Overview`, tags, time range, refresh (10s) | Grafana |
| Trạng thái nhanh | Nhìn sức khỏe tức thì | Stat/Status: `ApiGateway` Healthy/Unhealthy, `AsyncWorker` Healthy/Unhealthy, `DlqSize` hiện tại, `QueueDepth` hiện tại | `GET /api/v1/health` (hoặc probe), Prometheus `flash_sale_dlqsize`, `flash_sale_queuedepth` |
| P99Latency | Độ trễ fast-path/worker | Timeseries `P99Latency` theo `job` (`flashsale-api`, `flashsale-worker`) | Prometheus `flash_sale_p99latency` |
| ErrorRate | Tỷ lệ lỗi | Timeseries `ErrorRate` theo `job` | Prometheus `flash_sale_errorrate` |
| QueueDepth | Tải hàng đợi | Timeseries `QueueDepth` | Prometheus `flash_sale_queuedepth` |
| DlqSize | Message lỗi tồn đọng | Timeseries `DlqSize`, ngưỡng cảnh báo khi > 0 | Prometheus `flash_sale_dlqsize` |
| InventoryRemaining | Tồn kho còn lại | Timeseries `InventoryRemaining` | Prometheus `flash_sale_inventoryremaining` |
| DeadLetterQueue | Xem & xử lý DLQ | Bảng message (id, queue, reason/error, thời gian, retry count nếu có), bộ lọc, hành động `Retry`/`Purge` | RabbitMQ management / API ops / công cụ DLQ (thiết kế tích hợp hoặc deep-link) |

## 4. Luồng thao tác DevOps (Luồng B)

| Bước | Trạng thái UI | Mô tả | Ghi chú |
|---|---|---|---|
| 1 | Loading | Mở `Grafana Dashboard`, tải datasource/panel | Hiển thị skeleton/loading trên panel khi chưa có dữ liệu |
| 2 | Success / Empty | Xem `P99Latency`, `ErrorRate`, `QueueDepth`, `DlqSize`, `InventoryRemaining` | "Không có dữ liệu." nếu metric trống trong khoảng thời gian chọn |
| 3 | Success / Error | Kiểm tra `HealthCheck` `ApiGateway`, `AsyncWorker` | Nếu probe lỗi: "Không thể kết nối dịch vụ. Vui lòng kiểm tra ApiGateway/AsyncWorker." |
| 4 | Success / Empty | Xem `DeadLetterQueue` | DLQ trống: "Không có message lỗi nào trong DeadLetterQueue." |
| 5 | Confirm + Action | Chọn message(s) → `Retry` hoặc `Purge` | Hành động nguy hiểm (purge/xóa) yêu cầu xác nhận (modal) |
| 6 | Success / Error | Kết quả retry/purge | Toast/notice: thành công hoặc lỗi rõ nguyên nhân |

## 5. Thiết kế quản lý DeadLetterQueue

> Grafana core tập trung hiển thị metric; quản lý DLQ có thể thực hiện qua panel/table plugin, link tới RabbitMQ Management UI, hoặc trang ops riêng. Thiết kế yêu cầu khả năng tối thiểu:

- **Xem:** danh sách message trong `DeadLetterQueue` (metadata tối thiểu: message id, routing/queue, lỗi, thời gian, số lần retry nếu có), phân trang/lọc.
- **Retry:** chọn 1/nhiều message → xác nhận → thực hiện `Retry` (đưa lại luồng xử lý theo cơ chế hệ thống). Hiển thị trạng thái đang xử lý và kết quả.
- **Purge:** chọn 1/nhiều/tất cả (nếu cho phép) → modal xác nhận rõ "Hành động này không thể hoàn tác" → purge. Ghi nhận audit/log nếu hệ thống hỗ trợ.
- **Trạng thái bắt buộc:** loading, empty ("Không có message lỗi nào trong DeadLetterQueue."), error (không truy cập được DLQ), success.
- **Copy ví dụ:** "Có message lỗi cần xử lý trong DeadLetterQueue." khi `DlqSize > 0`.

## 6. Trạng thái hiển thị bắt buộc (tổng hợp)

| Ngữ cảnh | Trạng thái | Hiển thị |
|---|---|---|
| Metric/Panel | Loading | Skeleton/spinner trong panel, không để trống trắng |
| Metric/Panel | Empty | "Không có dữ liệu." (theo time range/filter) |
| Metric/Panel | Error | "Không kết nối được Prometheus/datasource." + hướng dẫn kiểm tra |
| Metric/Panel | Success | Biểu đồ/số liệu đầy đủ, legend theo `job` |
| HealthCheck | Healthy/Unhealthy | Badge xanh/đỏ rõ ràng cho `ApiGateway`, `AsyncWorker` |
| DeadLetterQueue | Empty | "Không có message lỗi nào trong DeadLetterQueue." |
| DeadLetterQueue | Có message | Cảnh báo khi `DlqSize > 0`, bảng/list hiển thị message |

## 7. Mapping hiện trạng và mở rộng

| Yêu cầu thiết kế | Hiện trạng | Ghi chú |
|---|---|---|
| 5 Metric trên Grafana | Đã có trong `FlashSale Overview` (timeseries, Prometheus) | `P99Latency`, `ErrorRate`, `QueueDepth`, `DlqSize`, `InventoryRemaining` theo `job` |
| HealthCheck hiển thị | Có endpoint `GET /api/v1/health` | Có thể bổ sung panel/stat qua datasource/JSON API hoặc link tới health endpoint |
| Quản lý DLQ (xem/retry/purge) | Chưa có UI Grafana tích hợp đầy đủ | Thiết kế yêu cầu; triển khai qua plugin/link/RabbitMQ UI hoặc ops API theo quyết định sau |
| Refresh/time range | `refresh: 10s`, `now-15m` | Phù hợp quan sát gần real-time |

## 8. Design tokens, accessibility, responsive

- **Màu/ngữ nghĩa (đề xuất):** Success `#10B981`, Warning `#F59E0B`, Error `#EF4444`, Primary `#0066CC`; dùng nhất quán cho badge/trạng thái metric và DLQ.
- **Ngôn ngữ:** UI copy tiếng Việt khi tùy biến; thuật ngữ nghiệp vụ giữ đúng `docs/domain-glossary.md` (`DeadLetterQueue`, `Retry`, `QueueDepth`, `InventoryRemaining`, ...).
- **Accessibility:** contrast tối thiểu 4.5:1, focus visible, thao tác bàn phím cho nút Retry/Purge/modal, label rõ cho input/filter.
- **Responsive:** desktop hiển thị nhiều panel song song; tablet/mobile xếp dọc, bảng DLQ cuộn ngang, nút tối thiểu 44x44px.

## 9. Ngoài phạm vi

- Triển khai plugin Grafana tùy chỉnh hoặc ứng dụng ops UI độc lập (chỉ thiết kế/spec trong story này).
- Thay đổi logic metric/health/backend ngoài nhu cầu hiển thị.
- Cấu hình alerting rule chi tiết (có thể bổ sung trong story/phase sau).
