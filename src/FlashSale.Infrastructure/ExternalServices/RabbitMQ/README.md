# RabbitMQ (FlashSale.Infrastructure/ExternalServices)

Triển khai publish message sang RabbitMQ cho luồng đặt hàng và sự kiện downstream.

## Thành phần
- `RabbitMqPublisherService.cs`: triển khai publish `OrderCreatedMessage` và message liên quan (exchange/queue/routing key theo `RabbitMqConstants`), hỗ trợ khai báo topology cần thiết.

## Quy tắc
- Dùng `RabbitMQ.Client` theo quyết định kiến trúc hiện tại.
- Tên exchange/queue/routing key phải khớp `FlashSale.Domain.RabbitMqConstants` và `docs/domain-glossary.md`.
- Không hardcode connection string; lấy từ cấu hình/environment.
