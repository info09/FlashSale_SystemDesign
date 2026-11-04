# Copilot Instructions — Distributed Flash-Sale Engine

File này cấu hình hành vi của Github Copilot cho toàn bộ dự án. Mọi session sau phải tuân theo. Không phải hướng dẫn một lần.

## 1. Quy tắc đọc file trước khi làm task

Trước KHI bắt đầu bất kỳ task nào (viết code mới, sửa bug, refactor), phải đọc lại đầy đủ các file sau theo thứ tự, không dựa vào ký ức từ session trước:

1. `docs/prd.md` — phần FR/NFR liên quan đến task.
2. `docs/domain-glossary.md` — toàn bộ (bất kỳ task nào chạm khái niệm nghiệp vụ đều cần đúng thuật ngữ).
3. `docs/architecture.md` — phần module liên quan đến task.
4. `docs/conventions.md` — toàn bộ.
5. `docs/status.md` — phase hiện tại và mục Deferred Work (để không lặp lại việc đã cố ý hoãn).
6. `docs/decisions.md` — rà xem có quyết định cũ nào liên quan đến task này, không đi ngược lại quyết định đã chốt.

Ngoài ra:
- Nếu task liên quan đến giao diện (tạo/sửa màn hình, component, form...): BẮT BUỘC đọc thêm `docs/ux.md` trước khi code, không tự sáng tạo pattern UI khác với đã định nghĩa.
- Nếu task liên quan đến build/deploy/release/CI-CD: BẮT BUỘC đọc thêm `docs/deploy.md` trước khi thực hiện.
- Nếu bất kỳ file nào trong 9 file bắt buộc (`docs/prd.md`, `docs/domain-glossary.md`, `docs/decisions.md`, `docs/architecture.md`, `docs/ux.md`, `docs/deploy.md`, `docs/conventions.md`, `docs/status.md`, `copilot-instructions.md`) không tồn tại hoặc trống khi lẽ ra phải có: DỪNG LẠI và báo cho người dùng, không tự suy diễn nội dung thay thế.

## 2. Định nghĩa "Done" (Definition of Done)

Một task CHỈ được coi là hoàn thành khi đồng thời có:
- (a) Unit test cho logic mới/thay đổi.
- (b) Integration test cho luồng liên quan.
- (c) E2e test cho hành vi người dùng cuối nếu task ảnh hưởng đến luồng user-facing.

Cả 3 tầng test đều phải pass. Thiếu 1 trong 3 tầng (khi tầng đó áp dụng được cho loại thay đổi này) thì task coi như CHƯA xong. Phải báo rõ cho người dùng là còn thiếu tầng test nào, không được báo "hoàn thành" khi chưa đủ.

## 3. Quy trình sau khi hoàn thành 1 task

Bắt buộc làm theo thứ tự:
- (a) Chạy đủ 3 tầng test và xác nhận pass.
- (b) Tick checklist tương ứng trong `docs/status.md`; đồng thời thêm vào mục Deferred Work nếu có việc bị hoãn lại trong quá trình làm.
- (c) Review code vừa viết theo checklist trong `docs/conventions.md` trước khi báo kết quả.

## 4. Quy tắc ghi quyết định vào `docs/decisions.md`

Bắt buộc: Mỗi khi phải chọn giữa nhiều cách làm hợp lý (chọn thư viện, đổi cách tiếp cận so với `docs/architecture.md` ban đầu, chọn giải pháp cho vấn đề chưa có tiền lệ trong dự án, hoặc quyết định bỏ qua 1 best practice vì lý do thực tế), PHẢI ghi ngay 1 entry vào `docs/decisions.md` theo đúng format đã định nghĩa trong file đó (Context/Options/Decision/Rationale/Trade-off/Status). Ghi TRƯỚC KHI báo hoàn thành task, không gộp lại ghi sau vì sẽ quên lý do thật.

Việc nhỏ không ảnh hưởng người khác (tên biến, format code...) thì không cần ghi, theo đúng ngưỡng đã định nghĩa trong `docs/decisions.md`.

## 5. Quy tắc kỷ luật thuật ngữ nghiệp vụ (`docs/domain-glossary.md`)

Bắt buộc: Mọi tên biến/class/entity/bảng dữ liệu/label UI đại diện cho 1 khái niệm nghiệp vụ PHẢI truy ngược được về đúng 1 thuật ngữ trong `docs/domain-glossary.md`.

Nếu task cần dùng 1 khái niệm nghiệp vụ chưa có trong glossary: PHẢI bổ sung vào `docs/domain-glossary.md` (đúng format: thuật ngữ chuẩn / định nghĩa / không được gọi là / thuật ngữ liên quan) TRƯỚC KHI viết code hoặc UI copy dùng tên đó. Không tự đặt tên tạm rồi dùng luôn. Không tự dịch hoặc diễn đạt khác đi so với những gì đã có trong glossary dù chỉ là viết tắt hay đồng nghĩa.

Nếu phát hiện code/UI hiện tại đang dùng tên khác với glossary (do task trước tạo ra hoặc do phát hiện lỗi): phải báo cho người dùng và hỏi có nên đổi lại cho khớp hay cập nhật glossary theo tên hiện có. Không tự ý sửa hàng loạt.

## 6. Quy tắc khi implement giao diện

Mọi màn hình/component mới phải khớp với nguyên tắc thiết kế, design token, và component pattern trong `docs/ux.md`.

Nếu task cần 1 pattern UI chưa có trong `docs/ux.md`: phải bổ sung vào `docs/ux.md` trước (và ghi lý do vào `docs/decisions.md` nếu đó là 1 lựa chọn đáng kể). Không tự tạo pattern rời rạc riêng cho từng màn hình.

## 7. Quy tắc khi thực hiện deploy/release

Phải làm đúng tuần tự trong `docs/deploy.md`.

Tuyệt đối KHÔNG tự ý deploy lên production khi chưa có xác nhận rõ ràng từ người dùng trong phiên làm việc hiện tại, kể cả khi mọi test đã pass và pipeline cho phép chạy tự động.

Deploy lên staging có thể tự thực hiện nếu pipeline cho phép. Production luôn cần xác nhận thủ công từ người dùng.

## 8. Quy tắc khi có mâu thuẫn giữa các file

Áp dụng cho MỌI cặp file. Nếu lúc implement phát hiện 2 file bắt buộc mô tả 2 cách làm khác nhau cho cùng 1 vấn đề, hoặc không thể đáp ứng đúng những gì 1 file đã mô tả do lý do kỹ thuật phát sinh: PHẢI dừng lại và hỏi người dùng. Không tự chọn 1 bên rồi tiếp tục. Không tự sửa file gốc để hợp thức hóa lựa chọn của mình.

Thứ tự ưu tiên tạm thời khi cần quyết định nhanh nhưng chưa hỏi kịp người dùng:
`docs/prd.md` và `docs/domain-glossary.md` (yêu cầu nghiệp vụ và tên gọi thống nhất) > `docs/architecture.md` / `docs/ux.md` / `docs/deploy.md` (thiết kế kỹ thuật) > `docs/conventions.md` (chi tiết code).

Đây chỉ là thứ tự tạm để không bị kẹt cứng. Quyết định cuối vẫn phải xác nhận lại với người dùng và ghi vào `docs/decisions.md` với Status: Active. Nếu người dùng đồng ý đổi hướng, phải đánh dấu quyết định cũ liên quan là Superseded.

## 9. Quy tắc khi docs/prd.md có mục [CẦN LÀM RÕ] chưa giải quyết liên quan đến task đang làm

Phải hỏi người dùng trước khi code. Không tự giả định.

## 10. Giới hạn phạm vi task

Không tự ý implement thêm FR chưa được yêu cầu trong task hiện tại, dù thấy "tiện làm luôn".
