# Thiết kế màn hình — Gọi API mua hàng (FlashSaleShopper)

**Story:** 7.1 (Phase 7, theo `docs/ux.md` mục 2 — Information Architecture)  
**Phạm vi:** Thiết kế màn hình/luồng cho `FlashSaleShopper` gọi API mua hàng `FlashSale` qua `ApiGateway`/`OrderPipeline`. Frontend hiện ngoài phạm vi MVP; tài liệu này là đặc tả thiết kế (UI spec/wireframe) để triển khai sau, đồng thời làm rõ mapping với phản hồi HTTP/JSON hiện tại.  
**Thuật ngữ:** dùng đúng `docs/domain-glossary.md` (FlashSaleShopper, FlashSale, ApiGateway, RateLimit, Inventory, OverSelling, Order, OrderPipeline, Queue, Message, Http202Accepted, BearerToken).

## 1. Mục tiêu và người dùng

- **Người dùng:** FlashSaleShopper (không rành kỹ thuật, cần phản hồi nhanh, rõ ràng).
- **Mục tiêu:** gửi yêu cầu mua `Product` trong `FlashSale`, nhận kết quả tức thì (thành công nhận đơn / hết hàng / vượt RateLimit / lỗi).
- **FR liên quan:** FR-01 (RateLimit), FR-02 (AtomicInventoryOffloading/Inventory), FR-03 (đẩy `Message` vào `Queue`, trả `Http202Accepted`), NFR-01/NFR-04.

## 2. Vị trí trong Information Architecture

```
Client (FlashSaleShopper)
└── Gọi API mua hàng (FlashSale)  ← màn hình này
    ├── Thành công → Http202Accepted (FR-03)
    ├── Hết hàng → Inventory không đủ / OverSelling (FR-02)
    ├── Vượt RateLimit → HTTP 429 (FR-01)
    └── Lỗi hệ thống → thông báo lỗi chung
```

## 3. Luồng tương tác (Flow)

| Bước | Trạng thái UI | Hành động hệ thống/API | Kết quả hiển thị |
|---|---|---|---|
| 1 | Idle (sẵn sàng) | Người dùng nhập/chọn `ProductId`, `Quantity`, nhấn "Mua ngay" | Form enabled |
| 2 | Loading | Gọi `POST /api/v1/flash-sale/buy` (header `Authorization: Bearer <token>`) | Disable nút, hiển thị loading/skeleton/spinner trên nút hoặc vùng kết quả |
| 3a | Error - RateLimit | API trả HTTP 429 (FR-01) | Thông báo vượt `RateLimit`, hướng dẫn đợi và thử lại |
| 3b | Error - Inventory | API trả HTTP 400, hết hàng/`INSUFFICIENT_INVENTORY` (FR-02) | Thông báo hết hàng, **không** hiểu là đã tạo `Order` |
| 3c | Success - nhận đơn | API trả HTTP 202 `Http202Accepted` (FR-03), message vào `Queue` | Thông báo "Đơn hàng đã được nhận và đang được xử lý" (chưa xử lý xong hoàn toàn) |
| 3d | Error - xác thực | HTTP 401/403 (token thiếu/không hợp lệ) | Yêu cầu đăng nhập lại/lấy `BearerToken` |
| 3e | Error - hệ thống | HTTP 5xx, timeout, lỗi mạng | Thông báo lỗi chung, cho phép thử lại |

## 4. Wireframe (mô tả bố cục)

### Desktop/Tablet (≥ 768px)
```
+------------------------------------------------------------------+
|  [Header/Brand]                         FlashSale                 |
+------------------------------------------------------------------+
|                                                                  |
|  Gọi API mua hàng                                                |
|  (mô tả ngắn: gửi yêu cầu mua Product trong FlashSale)           |
|                                                                  |
|  +------------------------------------------------------------+  |
|  |  Form mua hàng                                             |  |
|  |                                                            |  |
|  |  ProductId*   [ product-test___________________________ ]  |  |
|  |  Quantity*    [ 1________ ]                                |  |
|  |                                                            |  |
|  |  [ Mua ngay (Primary) ]        [ Đặt lại (Secondary) ]     |  |
|  +------------------------------------------------------------+  |
|                                                                  |
|  +------------------------------------------------------------+  |
|  |  Kết quả / Trạng thái (Toast hoặc Alert inline)            |  |
|  |  - Idle: trống / hướng dẫn                                 |  |
|  |  - Loading: "Đang gửi yêu cầu..." + spinner                |  |
|  |  - Success (202): badge Success + copy nhận đơn             |  |
|  |  - Error (429/400/401/5xx): badge Error + copy rõ ràng     |  |
|  |  - (nếu có) OrderId/CorrelationId hiển thị dạng metadata   |  |
|  +------------------------------------------------------------+  |
|                                                                  |
+------------------------------------------------------------------+
```

### Mobile (< 768px)
```
+------------------------------+
| FlashSale                    |
+------------------------------+
| Gọi API mua hàng             |
|                              |
| ProductId*                   |
| [__________________________] |
| Quantity*                    |
| [1_________________________] |
|                              |
| [     Mua ngay (full)     ]  |
| [     Đặt lại             ]  |
|                              |
| [ Kết quả / Alert ]          |
+------------------------------+
```

- Nút hành động chính tối thiểu 44x44px, dễ bấm.
- Kết quả hiển thị ngay dưới form (ưu tiên nhìn thấy trên màn hình nhỏ).

## 5. Thành phần UI (Component pattern)

| Thành phần | Quy tắc (theo `docs/ux.md` mục 5) | Áp dụng |
|---|---|---|
| Form | Label rõ ràng, thuật ngữ glossary; loading khi submit; lỗi validation dưới input | ProductId, Quantity (bắt buộc, Quantity >= 1) |
| Toast/Alert | Success/Error/Warning; lỗi có thể đóng/tự ẩn; copy đúng glossary | Phản hồi 202/429/400/401/5xx |
| Empty state | Hiển thị khi chưa có kết quả | "Chưa có yêu cầu mua hàng." hoặc vùng kết quả trống có hướng dẫn |
| Loading | Skeleton/spinner, không để nội dung trắng | Disable form + spinner trên nút "Mua ngay" |
| Modal | Không dùng cho thông báo đơn giản | Không bắt buộc trong màn hình này |

## 6. Trạng thái và copy bắt buộc

| Tình huống | HTTP/Code (API hiện tại) | Copy hiển thị (tiếng Việt) | Kiểu hiển thị |
|---|---|---|---|
| Đang gửi | — | "Đang gửi yêu cầu..." | Loading |
| Nhận đơn thành công | 202, `ORDER_ACCEPTED` | "Đơn hàng đã được nhận và đang được xử lý." | Success (không nói "đã hoàn tất") |
| Vượt RateLimit | 429 | "Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau." | Error/Warning |
| Hết hàng (Inventory) | 400, `INSUFFICIENT_INVENTORY` | "Sản phẩm đã hết hàng. Đơn hàng không được tạo." | Error |
| Chưa xác thực | 401 | "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." | Error |
| Lỗi hệ thống/mạng | 5xx/timeout | "Có lỗi xảy ra. Vui lòng thử lại sau." | Error |
| Validation (client) | — | "Vui lòng nhập ProductId." / "Quantity phải lớn hơn 0." | Error dưới input |

## 7. Mapping API (phục vụ implement sau)

- **Endpoint:** `POST /api/v1/flash-sale/buy`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <BearerToken>`
- **Request body:** `{ "productId": "string", "quantity": 1 }`
- **Response:**
  - `202`: `{ "message": "Don hang da duoc nhan va dang xu ly.", "orderId": "...", "code": "ORDER_ACCEPTED" }` → hiển thị copy nhận đơn (mục 6), có thể hiện `orderId`.
  - `400`: hết kho hoặc request không hợp lệ (`INSUFFICIENT_INVENTORY` / thông báo lỗi) → copy tương ứng.
  - `401`: chưa xác thực.
  - `429`: vượt `RateLimit`.

## 8. Design tokens & Accessibility (tóm tắt áp dụng)

- **Màu (đề xuất, `docs/ux.md` mục 4):** Primary `#0066CC`, Success `#10B981`, Error `#EF4444`, Warning `#F59E0B`, nền `#FFFFFF/#F9FAFB`, text `#111827/#6B7280`.
- **Typography:** Heading 20–24px, Body 14px, Caption 12px.
- **Spacing:** xs 4 / sm 8 / md 16 / lg 24 / xl 32 px.
- **Accessibility:** contrast ≥ 4.5:1 (text), focus visible, label gắn input (không chỉ placeholder), thao tác được bằng bàn phím (Tab/Enter/Space), thông báo lỗi không chỉ dựa vào màu sắc.

## 9. Ngoài phạm vi màn hình này

- Quản lý danh sách Product/Inventory, thanh toán, lịch sử `Order` chi tiết.
- Màn hình `Grafana Dashboard` và quản lý `DeadLetterQueue` (DevOps, luồng B).
- Implement frontend thực tế (Phase 7 chỉ thiết kế/spec trong MVP hiện tại).
