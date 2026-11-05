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

Chờ điền dần trong quá trình implement. Mỗi entry là 1 heading con để dễ tra cứu và link tới.
