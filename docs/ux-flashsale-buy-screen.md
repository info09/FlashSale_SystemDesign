# Thiết kế màn hình — Mua hàng / Giữ chỗ (FlashSaleShopper)

**Story:** 7.1 (UI hiện tại) + 8.2 (contract/spec cập nhật cho Phase 8, chưa implement)  
**Phạm vi:** Màn hình `/buy` cho `FlashSaleShopper`. Hiện tại triển khai luồng `POST /api/v1/flash-sale/buy` (chạy song song). Phase 8 bổ sung luồng **Hold/Reservation** theo `docs/plan-hold-reservation-flow.md` và quyết định 8.1.  
**Thuật ngữ:** dùng đúng `docs/domain-glossary.md` (`FlashSaleShopper`, `Inventory`, `OverSelling`, `Hold`, `HoldExpiry`, `InformationCustomer`, `Order`, `OrderPipeline`, `Http202Accepted`, `RateLimit`, `Outbox`).

## 1. Mục tiêu và người dùng

- **Người dùng:** FlashSaleShopper.
- **Mục tiêu:** mua sản phẩm trong `FlashSale` với phản hồi rõ ràng; hỗ trợ giữ chỗ 1 phút để nhập thông tin trước khi xác nhận.
- **FR/NFR liên quan:** FR-01, FR-02, FR-03, FR-04, NFR-01, NFR-04, NFR-06.

## 2. Vị trí trong Information Architecture

```
Client (FlashSaleShopper)
└── Mua hàng (/buy)
    ├── Luồng nhanh hiện tại (song song): POST /api/v1/flash-sale/buy
    │   ├── 202 Accepted
    │   ├── 400 INSUFFICIENT_INVENTORY
    │   ├── 429 RateLimit
    │   └── 401/5xx
    └── Luồng Hold (Phase 8)
        ├── Hold → kiểm tra Redis đủ quantity
        │   ├── Hết hàng → báo lỗi
        │   └── Còn hàng → giữ chỗ 60s + form thông tin
        ├── Confirm submit → Outbox pattern lưu InformationCustomer + publish → consumer tạo Order
        └── Hủy / hết hạn / reload → trả quantity, nhường chỗ
```

## 3. Luồng tương tác

### 3.1 Luồng hiện tại (`POST /api/v1/flash-sale/buy`, giữ song song)
- Nhập `productId`, `quantity` → submit.
- `202`: nhận đơn (`ORDER_ACCEPTED`, `orderId`).
- `400 INSUFFICIENT_INVENTORY`: hết hàng.
- `401`: đăng nhập lại.
- `429`: vượt RateLimit.
- `5xx`/mạng: lỗi hệ thống, thử lại.

### 3.2 Luồng Hold (Phase 8, thiết kế 8.2)

| Bước | Trạng thái UI | Hành động hệ thống/API | Kết quả hiển thị |
|---|---|---|---|
| 1 | Idle | Nhập `productId`, `quantity`, bấm "Mua ngay/Giữ chỗ" | Form enabled |
| 2 | Holding | `POST /api/v1/flash-sale/hold` `{productId, quantity}` | Disable nút, loading |
| 3a | OutOfStock | 400 `INSUFFICIENT_INVENTORY` (Redis không đủ quantity) | "Sản phẩm đã hết hàng." |
| 3b | Held | 200/201 `{holdId, expiresAt, ttlSeconds:60}` | Hiển thị form thông tin + countdown 60s + nút Xác nhận/Hủy giữ chỗ |
| 4 | Confirming | `POST /api/v1/flash-sale/hold/{holdId}/confirm` `{name, phoneNumber, address}` (bắt buộc) | Disable form, loading |
| 5a | Success | 202/200 (theo contract implement) sau khi tạo Outbox payload | "Đơn hàng đã được nhận và đang được xử lý." |
| 5b | Expired/Invalid | 409/410 (hết hạn/không hợp lệ/đã dùng) hoặc countdown = 0 / submit khi hết hạn | "Giữ chỗ đã hết hạn. Vui lòng giữ chỗ lại." (quantity được trả/nhường chỗ theo cơ chế hệ thống) |
| 5c | Cancelled | `POST /api/v1/flash-sale/hold/{holdId}/release` | Nhả slot, quay về Idle |
| 5d | Auth/RateLimit/Error | 401 / 429 / 400 validation / 5xx | Thông báo tương ứng |

**Ghi chú:** Reload trang làm mất trạng thái hold trên UI (không resume); slot phía server được nhả khi hết hạn/release (`FlashSale.WorkerUnHold` quét 1s). Submit chỉ hợp lệ trong thời gian hold và khi thông tin bắt buộc hợp lệ.

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

| Tình huống | HTTP/Code | Copy hiển thị (tiếng Việt) | Kiểu hiển thị |
|---|---|---|---|
| Đang gửi/giữ chỗ/xác nhận | — | "Đang gửi yêu cầu..." / "Đang giữ chỗ..." | Loading |
| Giữ chỗ thành công | 200/201 (hold) | "Bạn đã giữ chỗ thành công. Vui lòng nhập thông tin trong 1 phút." | Success/Info |
| Nhận đơn/submit thành công | 202 (`ORDER_ACCEPTED` hoặc tương đương) | "Đơn hàng đã được nhận và đang được xử lý." | Success |
| Hết hàng | 400 `INSUFFICIENT_INVENTORY` | "Sản phẩm đã hết hàng." | Error |
| Hết hạn giữ chỗ | 409/410 (hoặc timeout UI) | "Giữ chỗ đã hết hạn. Vui lòng giữ chỗ lại." | Warning/Error |
| Hold không hợp lệ/đã dùng | 409/410 | "Giữ chỗ không hợp lệ hoặc đã được sử dụng. Vui lòng giữ chỗ lại." | Error |
| User đã có hold active | 409 (đề xuất `HOLD_ALREADY_EXISTS`) | "Bạn đang có một giữ chỗ đang hoạt động. Vui lòng hoàn tất hoặc hủy giữ chỗ trước." | Warning/Error |
| Vượt RateLimit | 429 | "Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau." | Error/Warning |
| Chưa xác thực | 401 | "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." | Error |
| Validation | 400 | "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ." / lỗi trường cụ thể | Error dưới input |
| Lỗi hệ thống/mạng | 5xx/timeout | "Có lỗi xảy ra. Vui lòng thử lại sau." | Error |

## 7. Mapping API (contract 8.2)

### 7.1 Hiện tại (song song)
- `POST /api/v1/flash-sale/buy` body `{ "productId": "string", "quantity": 1 }`
- `202` `{ message, orderId, code: "ORDER_ACCEPTED", ... }`, `400 INSUFFICIENT_INVENTORY`, `401`, `429`.

### 7.2 Hold Flow (Phase 8)
- **Create hold:** `POST /api/v1/flash-sale/hold`
  - Req: `{ "productId": "string", "quantity": 1 }`
  - 200/201: `{ "holdId": "guid", "productId": "string", "quantity": 1, "expiresAt": "ISO-8601", "ttlSeconds": 60 }`
  - 400: `INSUFFICIENT_INVENTORY` | validation
  - 409: `HOLD_ALREADY_EXISTS` (đề xuất)
  - 401/429
- **Confirm:** `POST /api/v1/flash-sale/hold/{holdId}/confirm`
  - Req: `{ "name": "string", "phoneNumber": "string", "address": "string" }` (bắt buộc, non-empty/trim; giới hạn đề xuất Name≤200, PhoneNumber≤20, Address≤500)
  - 202/200: `{ "status":"accepted", "holdId":"guid", "orderId"|"correlationId":"guid", "message":"..." }` (chi tiết chốt khi implement)
  - 400: validation (thiếu/trống/sai định dạng)
  - 404/409/410: `HOLD_NOT_FOUND` | `HOLD_INVALID` | `HOLD_EXPIRED` | `HOLD_CONSUMED` | `HOLD_NOT_OWNED` (bộ mã lỗi cụ thể chốt khi implement)
  - 401/429/5xx
- **Release:** `POST /api/v1/flash-sale/hold/{holdId}/release`
  - 200/204: released/idempotent
  - 404/409/410: not found/invalid/expired/consumed (idempotent nếu đã release)

**Redis/Lua (thiết kế):** `inventory:{productId}`, `hold:{holdId}`, `hold:user:{clientId}`, `holds:expiring` (ZSET). Scripts: `hold_create.lua`, `hold_confirm.lua`, `hold_release.lua` hoặc `hold_release_if_expired.lua`.
**Outbox/message (flow chuẩn):** confirm tạo Outbox payload; Outbox processor lưu `InformationCustomer` và publish message; consumer tạo `Order` (idempotent). Payload đề xuất chứa `holdId`, `correlationId/orderId`, `productId`, `quantity`, `clientId`, `name`, `phoneNumber`, `address`, `occurredOn`.

## 8. Design tokens & Accessibility (tóm tắt áp dụng)

- **Màu (đề xuất, `docs/ux.md` mục 4):** Primary `#0066CC`, Success `#10B981`, Error `#EF4444`, Warning `#F59E0B`, nền `#FFFFFF/#F9FAFB`, text `#111827/#6B7280`.
- **Typography:** Heading 20–24px, Body 14px, Caption 12px.
- **Spacing:** xs 4 / sm 8 / md 16 / lg 24 / xl 32 px.
- **Accessibility:** contrast ≥ 4.5:1 (text), focus visible, label gắn input (không chỉ placeholder), thao tác được bằng bàn phím (Tab/Enter/Space), thông báo lỗi không chỉ dựa vào màu sắc.

## 9. Ngoài phạm vi màn hình này

- Quản lý danh sách Product/Inventory, thanh toán, lịch sử `Order` chi tiết.
- Màn hình `Grafana Dashboard` và quản lý `DeadLetterQueue` (DevOps, luồng B).
- Implement frontend thực tế (Phase 7 chỉ thiết kế/spec trong MVP hiện tại).
