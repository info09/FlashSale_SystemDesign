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
- `VUS` (mặc định `8000`)
- `DURATION` (mặc định `30s`)
- `SLEEP_SECONDS` (mặc định `0.1`)
- `THRESHOLD_P99_MS`: nếu đặt (vd `20`), bật threshold `http_req_duration p(99)<THRESHOLD_P99_MS` (Story 6.2, NFR-01)
- `THRESHOLD_HTTP_REQ_FAILED_RATE`: ngưỡng `http_req_failed` rate (mặc định `1`; k6 tính 4xx/5xx là failed)

## Lưu ý
- Story 6.1: script giả lập 8.000 VU đồng thời. Story 6.2–6.4 dùng cùng script với threshold/env phù hợp để kiểm tra NFR-01/NFR-02/NFR-03.
- Cần đảm bảo ngưỡng RateLimit, số lượng Inventory, token hợp lệ, và dữ liệu test phù hợp kịch bản benchmark để kết quả phản ánh đúng mục tiêu.
