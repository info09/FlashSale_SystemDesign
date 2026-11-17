# Commands — RateLimit (Application/Features)

Chứa command cho nghiệp vụ kiểm tra giới hạn tần số.

## File
- `RateLimitCommand.cs`: dữ liệu đầu vào để kiểm tra RateLimit.

## Quy tắc
- Command mang ngữ cảnh cần thiết (client/key/request info theo thiết kế) để service đánh giá token bucket.
- Thuật ngữ chuẩn: `RateLimit`, `TokenBucket`.
