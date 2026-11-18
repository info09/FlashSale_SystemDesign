# k6 Load Test Scripts

Thư mục chứa script k6 cho Phase 6 (FR-07, NFR-01, NFR-02, NFR-03).

## Script hiện có
- `flashsale-buy.js` — Story 6.1: giả lập **8.000 VirtualUser đồng thời** gọi `POST /api/v1/flash-sale/buy`.

## Chuẩn bị
- API/Worker và hạ tầng (PostgreSQL, Redis, RabbitMQ) đang chạy và đã cấu hình dữ liệu cần thiết (user/token, `PRODUCT_ID`, tồn kho/Inventory trong Redis).
- Endpoint mua hàng yêu cầu Bearer token (`Authorization: Bearer <JWT>`).

## Chạy với Docker Compose
Từ thư mục gốc project (có `docker-compose.yml`):

```bash
docker compose --profile k6 run --rm \
  -e BASE_URL=http://flashsale-api:8080 \
  -e ACCESS_TOKEN=<JWT_TOKEN> \
  -e PRODUCT_ID=flashsale-product-1 \
  -e VUS=8000 \
  -e DURATION=30s \
  flashsale-k6 run /scripts/flashsale-buy.js
```

Hoặc để script tự login trong `setup()` (1 token dùng chung cho các VU, có thể bị rate limit theo client):

```bash
docker compose --profile k6 run --rm \
  -e BASE_URL=http://flashsale-api:8080 \
  -e K6_USERNAME=k6user \
  -e K6_PASSWORD='P@ssw0rd123!' \
  -e K6_EMAIL=k6user@example.com \
  -e PRODUCT_ID=flashsale-product-1 \
  flashsale-k6 run /scripts/flashsale-buy.js
```

## Biến môi trường
- `BASE_URL` (mặc định `http://flashsale-api:8080`)
- `ACCESS_TOKEN` / `TOKEN`: JWT Bearer token
- `K6_USERNAME`, `K6_PASSWORD`, `K6_EMAIL`: thông tin login/register trong `setup()` nếu không truyền token
- `PRODUCT_ID` (mặc định `flashsale-product-1`)
- `QUANTITY` (mặc định `1`)
- `EXECUTOR`: `constant-vus` (mặc định) | `constant-arrival-rate` | `ramping-arrival-rate`
- `VUS` (mặc định `8000`, cho `constant-vus`), `DURATION` (mặc định `30s`), `SLEEP_SECONDS` (mặc định `0.1`, `constant-vus`)
- `RATE`, `TIME_UNIT` (mặc định `1s`), `PREALLOCATED_VUS`, `MAX_VUS`: cho arrival-rate (Story 6.3 spike RPS, vd `EXECUTOR=constant-arrival-rate RATE=15000`)
- `START_RATE`, `RAMP_UP_DURATION`, `HOLD_DURATION`, `RAMP_DOWN_DURATION`, `END_RATE`: cho `ramping-arrival-rate`
- `THRESHOLD_P99_MS`: nếu đặt (vd `20`), bật threshold `http_req_duration p(99)<THRESHOLD_P99_MS` (Story 6.2, NFR-01)
- `THRESHOLD_HTTP_REQ_FAILED_RATE`: ngưỡng `http_req_failed` rate (mặc định `1`; k6 tính 4xx/5xx là failed)
- `THRESHOLD_CHECKS_RATE`: nếu đặt (vd `1`), yêu cầu mọi response thuộc {202,400,401,429} — không có 5xx do quá tải (Story 6.3, NFR-02)
- `THRESHOLD_ERROR_RATE`: nếu đặt (vd `0.01`), yêu cầu `error_rate < THRESHOLD_ERROR_RATE` (5xx/timeout/response bất thường; không tính 202/400/401/429) — Story 6.4, NFR-03

## Ví dụ Story 6.2 (p99 < 20ms)
```bash
docker compose --profile k6 run --rm \
  -e BASE_URL=http://flashsale-api:8080 -e ACCESS_TOKEN=<JWT> -e PRODUCT_ID=<id> \
  -e VUS=10 -e DURATION=30s -e SLEEP_SECONDS=0 -e THRESHOLD_P99_MS=20 \
  flashsale-k6 run /scripts/flashsale-buy.js
```

## Ví dụ Story 6.3 (spike 15.000 RPS, không 5xx ngoài 429)
```bash
docker compose --profile k6 run --rm \
  -e BASE_URL=http://flashsale-api:8080 -e ACCESS_TOKEN=<JWT> -e PRODUCT_ID=<id> \
  -e EXECUTOR=constant-arrival-rate -e RATE=15000 -e DURATION=30s \
  -e PREALLOCATED_VUS=2000 -e MAX_VUS=15000 -e SLEEP_SECONDS=0 \
  -e THRESHOLD_CHECKS_RATE=1 -e THRESHOLD_HTTP_REQ_FAILED_RATE=1 \
  flashsale-k6 run /scripts/flashsale-buy.js
```

## Ví dụ Story 6.4 (Error Rate < 1% với 8.000 VU)
```bash
docker compose --profile k6 run --rm \
  -e BASE_URL=http://flashsale-api:8080 -e ACCESS_TOKEN=<JWT> -e PRODUCT_ID=<id> \
  -e VUS=8000 -e DURATION=30s -e SLEEP_SECONDS=0.1 \
  -e THRESHOLD_ERROR_RATE=0.01 -e THRESHOLD_CHECKS_RATE=0.99 \
  flashsale-k6 run /scripts/flashsale-buy.js
```

## Lưu ý
- Story 6.1: 8.000 VU đồng thời. Story 6.2–6.4 dùng cùng script với threshold/env phù hợp để kiểm tra NFR-01/NFR-02/NFR-03.
- `error_rate` (custom) không tính 202/400/401/429 là lỗi; `http_req_failed` của k6 vẫn tính 4xx/5xx. 15.000 RPS và 8.000 VU đòi hỏi hạ tầng/máy chạy k6 đủ mạnh.
