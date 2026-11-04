# UX/UI Guideline — Distributed Flash-Sale Engine & Event-Driven Order Pipeline

## 1. Nguyên tắc thiết kế

Các nguyên tắc dưới đây gắn trực tiếp với persona trong `docs/prd.md` (phần Đối tượng người dùng). Không viết nguyên tắc chung chung.

1. **FlashSaleShopper ưu tiên tốc độ và rõ ràng:** Người dùng không rành kỹ thuật, thao tác đặt mua cực nhanh, không chấp nhận hệ thống báo thành công sau đó hủy do hết hàng. Mọi phản hồi từ API phải có thể hiển thị thành kết quả thành công/thất bại ngay lập tức, không dùng thuật ngữ chuyên môn kỹ thuật trong copy hiển thị cho người dùng cuối.
2. **Không làm mờ trạng thái đơn hàng:** Vì hệ thống trả `Http202Accepted` ngay (không chờ xử lý hoàn tất), UI nếu có phải cho người dùng biết rõ đơn hàng đã được nhận nhưng chưa xử lý xong, không hiểu nhầm là đơn đã thành công hoàn toàn.
3. **DevOps cần nhìn nắm được toàn cảnh:** `Kỹ sư vận hành hệ thống` cần theo dõi `RateLimit`, `Inventory`, `QueueDepth`, `DlqSize`, `ErrorRate` trên `Grafana Dashboard` và có thể quản lý `DeadLetterQueue` (xem, retry, purge) mà không cần đi vào code.
4. **Không Bury lỗi:** Mọi trạng thái lỗi (hết hàng, rate limit vượt ngưỡng, lỗi hệ thống) phải hiển thị rõ ràng ngay trên màn hình/client, không để người dùng suy đoán.
5. **Giới hạn tần số trongmeye trước:** `RateLimit` phải được truyền达 đến người dùng khi họ bị chặn, để tránh việc bấm lại liên tục làm nặng thêm hệ thống (liên quan NFR-02, NFR-04).

[ĐỀ XUẤT - CẦN DUYỆT] Vì `docs/prd.md` và `docs/architecture.md` khẳng định UI/Frontend nằm ngoài phạm vi MVP, nguyên tắc trên hiện tại áp dụng cho phản hồi HTTP (status code + JSON body) mà `FlashSaleShopper` nhận, và cho `Grafana Dashboard` mà `DevOps` sử dụng. Nếu sau này bổ sung UI frontend, nguyên tắc này sẽ được chuyển sang giao diện trực quan.

## 2. Information Architecture

Sơ đồ cây điều hướng (dựa trên thành phần đã chốt trong `docs/architecture.md`). Mỗi nút map với FR trong `docs/prd.md`.

```
Client (FlashSaleShopper / FlashSaleShopper)
└── Gọi API mua hàng (FlashSale)
    ├── Thành công → nhận Http202Accepted (FR-03)
    ├── Hết hàng → thông báo Inventory không đủ (FR-02)
    ├── Vượt RateLimit → thông báo HTTP 429 (FR-01)
    └── Lỗi hệ thống → thông báo lỗi chung

DevOps (Kỹ sư vận hành hệ thống)
└── Grafana Dashboard
    ├── Xem Metric: P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining (FR-06, NFR-07)
    ├── Xem HealthCheck của ApiGateway và AsyncWorker (FR-06)
    └── Quản lý DeadLetterQueue: xem, retry, purge (FR-05, NFR-07)
```

**Tên màn hình/chính theo đúng thuật ngữ trong `docs/domain-glossary.md`:**

| Tên màn hình / thành phần hiển thị | Thuật ngữ chuẩn (code) | FR liên quan | Người dùng chính |
|------------------------------------|----------------------|--------------|------------------|
| Gọi API mua hàng | `ApiGateway` + `OrderPipeline` | FR-01, FR-02, FR-03 | FlashSaleShopper |
| Phản hồi Http202Accepted | `Http202Accepted` | FR-03 | FlashSaleShopper |
| Thông báo hết hàng | `Inventory` / `OverSelling` | FR-02 | FlashSaleShopper |
| Thông báo vượt RateLimit | `RateLimit` | FR-01 | FlashSaleShopper |
| Grafana Dashboard | `Grafana` | FR-06, NFR-07 | DevOps |
| Quản lý DeadLetterQueue | `DeadLetterQueue` | FR-05, NFR-07 | DevOps |
| HealthCheck | `HealthCheck` | FR-06 | DevOps |

[ĐỀ XUẤT - CẦN DUYỆT] Vì UI frontend out of scope, "màn hình" ở đây hiện tại là các endpoint API và dashboard `Grafana` đã có sẵn, không phải trang web mới thiết kế.

## 3. Danh sách màn hình/luồng chính

Vì UI frontend out of scope trong MVP, luồng chính hiện tại là phản hồi HTTP/API mà `FlashSaleShopper` nhận. Dưới đây là mô tả các trạng thái bắt buộc phải có khi implement (dù là JSON body hay giao diện trực quan sau này).

### Luồng A: FlashSaleShopper đặt mua hàng (FR-01, FR-02, FR-03)

| Bước | Trạng thái | Mô tả | Thuật ngữ hiển thị bắt buộc |
|------|-----------|-------|---------------------------|
| 1 | Gửi request | Người dùng nhấn nút mua hàng trong `FlashSale` | — |
| 2 | Kiểm tra RateLimit | `ApiGateway` kiểm tra `RateLimit` qua `TokenBucket` | — |
| 3a | Lỗi: vượt RateLimit | Trả HTTP 429, thông báo "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau." | `RateLimit` |
| 3b | Tiếp tục | Cho phép xử lý tiếp | — |
| 4 | Kiểm tra Inventory | `AtomicInventoryOffloading` trừ kho trên Redis qua `LuaScript` | — |
| 5a | Lỗi: hết hàng | Trả lỗi "Sản phẩm đã hết hàng." Không tạo `Order`. | `Inventory`, `OverSelling` |
| 5b | Thành công | Trừ kho thành công | — |
| 6 | Đẩy vào Queue | Đưa `Message` vào `Queue` trên `RabbitMQ` | `OrderPipeline`, `Queue` |
| 7 | Trả về client | Trả `Http202Accepted` ngay, không chờ xử lý xong | `Http202Accepted` |
| 8 | Trạng thái đơn | Nếu có UI hiển thị: "Đơn hàng đã được nhận, đang xử lý." | `Order` |

**Trạng thái bắt buộc không được thiếu:** loading (đang xử lý request), empty (không có dữ liệu), error (hết hàng, vượt rate limit, lỗi hệ thống), success (đã nhận 202 Accepted).

### Luồng B: DevOps theo dõi hệ thống qua Grafana (FR-06, NFR-07)

| Bước | Trạng thái | Mô tả | Thuật ngữ hiển thị bắt buộc |
|------|-----------|-------|---------------------------|
| 1 | Mở Dashboard | `DevOps` truy cập `Grafana Dashboard` | `Grafana` |
| 2 | Xem Metric | Xem `P99Latency`, `ErrorRate`, `QueueDepth`, `DlqSize`, `InventoryRemaining` | `Metric` |
| 3 | Kiểm tra HealthCheck | Xem trạng thái `ApiGateway`, `AsyncWorker` | `HealthCheck` |
| 4 | Quản lý DLQ | Xem message trong `DeadLetterQueue`, chọn retry hoặc purge | `DeadLetterQueue`, `Retry` |

**Trạng thái bắt buộc không được thiếu:** loading (đang lấy metric), empty (không có metric / DLQ trống), error (không kết nối được Prometheus), success (hiển thị metric đầy đủ).

## 4. Design tokens cơ bản

[ĐỀ XUẤT - CẦN DUYỆT] Chưa có brand guideline. Dưới đây là baseline hợp lý dựa trên persona (FlashSaleShopper cần tốc độ, DevOps cần nhìn nắm được metric). Mọi giá trị này cần được bạn duyệt lại trước khi áp dụng vào code/UI.

### Bảng màu

| Mục đích | Mã màu (đề xuất) | Ghi chú |
|----------|-----------------|---------|
| Primary | `#0066CC` | Dùng cho nút hành động chính, nhấn nháy |
| Secondary | `#6B7280` | Dùng cho nút phụ, văn bản thứ cấp |
| Success | `#10B981` | Thông báo thành công (ví dụ: đã nhận `Http202Accepted`) |
| Warning | `#F59E0B` | Cảnh báo (ví dụ: sắp hết `Inventory`, gần vượt `RateLimit`) |
| Error | `#EF4444` | Lỗi (hết hàng, vượt `RateLimit`, lỗi hệ thống) |
| Background | `#FFFFFF` / `#F9FAFB` | Nền chính và nền phụ |
| Text primary | `#111827` | Văn bản chính |
| Text secondary | `#6B7280` | Văn bản mô tả, chú thích |

### Typography scale

| Loại | Kích thước (đề xuất) | Trọng số | Dùng cho |
|------|---------------------|----------|----------|
| Heading 1 | 24px | 700 | Tiêu đề trang chính |
| Heading 2 | 20px | 600 | Tiêu đề nhóm |
| Body | 14px | 400 | Văn bản chính, thông báo |
| Caption | 12px | 400 | Chú thích nhỏ, metadata |
| Overline | 10px | 600 | Label phân loại, status chip |

### Spacing scale

| Tên | Giá trị (đề xuất) | Dùng cho |
|-----|-------------------|----------|
| xs | 4px | Padding bên trong nút nhỏ, khoảng cách icon |
| sm | 8px | Khoảng cách giữa các thành phần gần nhau |
| md | 16px | Padding mặc định của card, form |
| lg | 24px | Khoảng cách giữa các section lớn |
| xl | 32px | Khoảng cách giữa các màn hình chính |

### Breakpoint responsive

| Breakpoint | Giá trị (đề xuất) | Dùng cho |
|------------|-------------------|----------|
| Mobile | < 768px | Điện thoại |
| Tablet | 768px – 1023px | Máy tính bảng |
| Desktop | ≥ 1024px | Máy tính để bàn |

## 5. Component pattern chung

Quy tắc cho các thành phần lặp lại, để không tự sáng tạo pattern mới mỗi lần implement.

### Form
- Mỗi input phải có label rõ ràng, dùng thuật ngữ chuẩn từ `docs/domain-glossary.md`.
- Khi submit, hiển thị trạng thái loading (skeleton hoặc spinner) cho đến khi nhận phản hồi.
- Nếu lỗi validation, hiển thị message lỗi ngay dưới input liên quan, dùng màu Error.
- Không tự ý đặt tên label khác so với glossary (ví dụ: không gọi "Số lượng tồn kho" là "Stock" hay "Quantity" nếu thuật ngữ chuẩn là `Inventory`).

### Table
- Dùng để hiển thị danh sách `Order`, `Metric`, hoặc message trong `DeadLetterQueue`.
- Có cột trạng thái với badge màu (Success / Warning / Error).
- Có trạng thái empty khi không có dữ liệu: hiển thị "Không có dữ liệu." kèm biểu tượng trống.

### Modal
- Chỉ dùng khi cần xác nhận hành động quan trọng (ví dụ: xóa message trong `DeadLetterQueue`, retry `Retry`).
- Có nút đóng rõ ràng (X) và nút hành động chính (Primary) / nút hủy (Secondary).
- Không dùng modal cho thông báo đơn giản, dùng toast thay thế.

### Toast / Notification
- Hiển thị thông báo tạm thời sau hành động (thành công, lỗi, cảnh báo).
- Thông báo lỗi phải dùng màu Error và có nút "Đóng" hoặc tự biến mất sau 5 giây.
- Copy phải dùng thuật ngữ chuẩn từ glossary (ví dụ: "Đã nhận đơn hàng" thay vì "Đặt hàng thành công" nếu trạng thái thực tế chỉ là `Http202Accepted`).

### Empty state
- Mọi danh sách có thể rỗng phải có empty state: ilustrasi đơn giản + văn bản giải thích + nút hành động (nếu có).
- Ví dụ: `DeadLetterQueue` trống → "Không có message lỗi nào."; `Inventory` hết → "Sản phẩm đã hết hàng."

### Loading skeleton
- Dùng skeleton thay vì spinner toàn trang khi đang tải dữ liệu metric trên `Grafana Dashboard` hoặc danh sách `Order`.
- Không hiển thị nội dung trắng nhilih khi chưa có dữ liệu.

## 6. Accessibility tối th الأدنى

Mức tối thiểu chấp nhận được (không viết như checklist WCAG đầy đủ vì dự án tập trung backend API):

1. **Contrast tối thiểu:** Văn bản chính đối với nền phải có tỷ trọng tương phản tối thiểu 4.5:1; văn bản lớn (heading) tối thiểu 3:1.
2. **Keyboard navigation:** Mọi nút và input phải có thể focus và thao tác bằng phím Tab, Enter, Space. Không chỉ dùng chuột.
3. **Label cho input:** Mỗi input phải có `<label>` gắn liền, không chỉ dùng placeholder làm thay thế label.
4. **Alt text cho ảnh:** Mọi ảnh biểu tượng hoặc ilustrasi phải có alt text mô tả ý nghĩa.
5. **Focus visible:** Phần tử đang focus phải có viền hoặc nền nổi bật rõ ràng để người dùng biết vị trí hiện tại.

## 7. Content / Tone of voice trong UI copy

### Cách xưng hô
- Gọi người dùng là "bạn" (trong ngữ cảnh FlashSaleShopper).
- Gọi người vận hành là "bạn" hoặc "quản trị viên" (trong ngữ cảnh DevOps).
- Không dùng từ xa xí, không dùng từ ngữ marketing hùng hỵ.

### Mức độ trang trọng
- Trung lập, trực tiếp, không lan man.
- Thông báo lỗi phải nói rõ nguyên nhân và hành động người dùng nên làm (ví dụ: "Sản phẩm đã hết hàng. Vui lòng thử lại sau." thay vì "Lỗi hệ thống.").

### Ngôn ngữ
- UI hiển thị tiếng Việt (theo persona và yêu cầu của bạn).
- Code viết tiếng Anh.
- Mọi danh từ nghiệp vụ trong copy phải khớp nguyên văn với `docs/domain-glossary.md`. Ví dụ:
  - Đúng: "Sản phẩm đã hết hàng." (dùng từ `Inventory`)
  - Sai: "Hết stock.", "Không đủ quantity.", "Over-selling xảy ra." (không dùng từ đồng nghĩa không nằm trong glossary)

### Ví dụ copy bắt buộc

| Tình huống | Copy đúng (tiếng Việt) | Thuật ngữ chuẩn đính kèm |
|------------|------------------------|--------------------------|
| Vượt RateLimit | "Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau." | `RateLimit` |
| Hết hàng | "Sản phẩm đã hết hàng. Đơn hàng không được tạo." | `Inventory`, `OverSelling` |
| Nhận đơn thành công | "Đơn hàng đã được nhận và đang được xử lý." | `Order`, `Http202Accepted` |
| Lỗi hệ thống | "Có lỗi xảy ra. Vui lòng thử lại sau." | — |
| DLQ có message | "Có message lỗi cần xử lý trong DeadLetterQueue." | `DeadLetterQueue` |
| Inventory thấp | "Cảnh báo: tồn kho Inventory đang thấp." | `Inventory`, `InventoryRemaining` |

## 8. Responsive rule

[ĐỀ XUẤT - CẦN DUYỆT] Vì `docs/architecture.md` khẳng định không có frontend/UI trong MVP, quy tắc responsive dưới đây là dự bị cho khi UI được bổ sung sau này.

| Thiết bị | Hành vi tối thiểu |
|----------|-------------------|
| Mobile (< 768px) | Menu thu gọn thành hamburger, các nút hành động chính phải đủ lớn để bấm ngón tay (tối thiểu 44x44px), bảng metric co dọc để đọc được trên màn hình nhỏ. |
| Tablet (768px – 1023px) | Hiển thị đầy đủ bảng metric, điều hướng có thể dùng sidebar hoặc tab ngang. |
| Desktop (≥ 1024px) | Hiển thị toàn bộ dashboard `Grafana` với nhiều panel metric cùng lúc, có thể mở nhiều cửa sổ quản lý `DeadLetterQueue` song song. |

**Quy tắc đặt tên hiển thị trong UI:** Nếu cần dùng 1 thuật ngữ hiển thị chưa có trong `docs/domain-glossary.md`, PHẢI bổ sung vào glossary trước (đúng format 4 cột), không được tự đặt tên riêng cho UI. Sau khi bổ sung, ghi quyết định vào `docs/decisions.md` nếu có nhiều cách gọi hợp lý cần chọn 1.

---

File này sẽ được `docs/conventions.md` tham chiếu khi định nghĩa naming/cấu trúc cho component, và được `copilot-instructions.md` bắt buộc đọc trước khi code bất kỳ màn hình nào.
