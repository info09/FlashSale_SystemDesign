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

Chờ điền dần trong quá trình implement. Mỗi entry là 1 heading con để dễ tra cứu và link tới.
