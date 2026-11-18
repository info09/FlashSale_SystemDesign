import http from 'k6/http';
import { check, sleep } from 'k6';

// Story 6.1 (FR-07, NFR-03): script k6 giả lập 8.000 VirtualUser đồng thời.
// Cấu hình qua biến môi trường:
// - BASE_URL: URL gốc API (vd: http://flashsale-api:8080 trong docker-compose network, hoặc http://localhost:8080)
// - ACCESS_TOKEN: Bearer token JWT (ưu tiên). Nếu không có, script thử login trong setup().
// - K6_USERNAME / K6_PASSWORD: dùng để login/register trong setup() khi không có ACCESS_TOKEN.
// - K6_EMAIL: email dùng khi register (mặc định: <username>@example.com)
// - PRODUCT_ID: mã sản phẩm mua (mặc định: flashsale-product-1)
// - QUANTITY: số lượng mua mỗi request (mặc định: 1)
// - VUS: số VirtualUser đồng thời (mặc định: 8000)
// - DURATION: thời gian chạy tải đồng thời (mặc định: 30s)
// - SLEEP_SECONDS: nghỉ giữa các lần mua của mỗi VU (mặc định: 0.1)

const BASE_URL = __ENV.BASE_URL || 'http://flashsale-api:8080';
const PRODUCT_ID = __ENV.PRODUCT_ID || 'flashsale-product-1';
const QUANTITY = Number(__ENV.QUANTITY || 1);
const VUS = Number(__ENV.VUS || 8000);
const DURATION = __ENV.DURATION || '30s';
const SLEEP_SECONDS = Number(__ENV.SLEEP_SECONDS || 0.1);
const THRESHOLD_P99_MS = __ENV.THRESHOLD_P99_MS ? Number(__ENV.THRESHOLD_P99_MS) : null;
const THRESHOLD_HTTP_REQ_FAILED_RATE = __ENV.THRESHOLD_HTTP_REQ_FAILED_RATE
  ? Number(__ENV.THRESHOLD_HTTP_REQ_FAILED_RATE)
  : 1;

const thresholds = {
  http_req_failed: [`rate<${THRESHOLD_HTTP_REQ_FAILED_RATE}`],
};

// Story 6.2 (NFR-01): p99 latency luồng nhận đơn < 20ms (fast-path).
if (THRESHOLD_P99_MS !== null && !Number.isNaN(THRESHOLD_P99_MS)) {
  thresholds.http_req_duration = [`p(99)<${THRESHOLD_P99_MS}`];
}

export const options = {
  scenarios: {
    flash_sale_buy: {
      executor: 'constant-vus',
      vus: VUS,
      duration: DURATION,
    },
  },
  thresholds,
};

export function setup() {
  if (__ENV.ACCESS_TOKEN) {
    return { accessToken: __ENV.ACCESS_TOKEN };
  }

  const username = __ENV.K6_USERNAME;
  const password = __ENV.K6_PASSWORD;
  if (!username || !password) {
    return { accessToken: null };
  }

  const email = __ENV.K6_EMAIL || `${username}@example.com`;
  const headers = { 'Content-Type': 'application/json' };

  // Thử đăng ký (bỏ qua lỗi nếu user đã tồn tại), sau đó đăng nhập để lấy token.
  http.post(
    `${BASE_URL}/api/v1/auth/register`,
    JSON.stringify({ userName: username, email: email, password: password }),
    { headers }
  );

  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ userName: username, password: password }),
    { headers }
  );

  let accessToken = null;
  try {
    const body = loginRes.json();
    accessToken = body && body.accessToken ? body.accessToken : null;
  } catch (_) {
    accessToken = null;
  }

  return { accessToken };
}

export default function (data) {
  const token = (data && data.accessToken) || __ENV.ACCESS_TOKEN || __ENV.TOKEN;
  const headers = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const payload = JSON.stringify({
    productId: PRODUCT_ID,
    quantity: QUANTITY,
  });

  const res = http.post(`${BASE_URL}/api/v1/flash-sale/buy`, payload, { headers });

  // 202: nhận đơn thành công; 400: hết kho/không hợp lệ; 401: chưa xác thực; 429: rate limit.
  // 5xx (ngoại 429) là dấu hiệu quá tải/lỗi hệ thống cần theo dõi ở các story benchmark sau.
  check(res, {
    'status is expected (202/400/401/429)': (r) => [202, 400, 401, 429].includes(r.status),
  });

  if (SLEEP_SECONDS > 0) {
    sleep(SLEEP_SECONDS);
  }
}
