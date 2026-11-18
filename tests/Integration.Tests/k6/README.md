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

## Lưu ý
- Story 6.1 chỉ yêu cầu **viết script** giả lập 8.000 VU đồng thời. Việc đo p99 latency, spike 15.000 RPS, error rate < 1% và đối chiếu success metrics thuộc các story 6.2–6.6.
- Cần đảm bảo ngưỡng RateLimit, số lượng Inventory, và chiến lược cấp token phù hợp kịch bản benchmark để kết quả phản ánh đúng mục tiêu.
