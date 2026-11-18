import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Story 6.4 (NFR-03): Error Rate < 1% (loi he thong/5xx hoac response khong mong doi), khong tinh 429/400/401/202 hop le.
const errorRate = new Rate('error_rate');

// Story 6.1 (FR-07, NFR-03): script k6 giả lập 8.000 VirtualUser đồng thời.
// Cấu hình qua biến môi trường:
// - BASE_URL: URL gốc API (vd: http://flashsale-api:8080 trong docker-compose network, hoặc http://localhost:8080)
// - ACCESS_TOKEN: Bearer token JWT (ưu tiên). Nếu không có, script thử login trong setup().
// - K6_USERNAME / K6_PASSWORD: dùng để login/register trong setup() khi không có ACCESS_TOKEN.
// - K6_EMAIL: email dùng khi register (mặc định: <username>@example.com)
// - PRODUCT_ID: mã sản phẩm mua (mặc định: flashsale-product-1)
// - QUANTITY: số lượng mua mỗi request (mặc định: 1)
// - EXECUTOR: constant-vus (mặc định) | constant-arrival-rate | ramping-arrival-rate
// - VUS: số VirtualUser đồng thời cho constant-vus (mặc định: 8000)
// - DURATION: thời gian chạy (mặc định: 30s)
// - RATE: số iteration/request bắt đầu mỗi timeUnit cho arrival-rate (vd 15000 cho Story 6.3)
// - TIME_UNIT: đơn vị thời gian cho arrival-rate (mặc định: 1s)
// - PREALLOCATED_VUS: VU cấp sẵn cho arrival-rate (mặc định: 2000)
// - MAX_VUS: VU tối đa cho arrival-rate (mặc định: 15000)
// - SLEEP_SECONDS: nghỉ giữa các lần mua của mỗi VU (mặc định: 0.1, dùng cho constant-vus)
// - THRESHOLD_P99_MS: nếu đặt (vd 20), bật threshold p(99) http_req_duration (Story 6.2)
// - THRESHOLD_HTTP_REQ_FAILED_RATE: ngưỡng http_req_failed rate (mặc định: 1)
// - THRESHOLD_CHECKS_RATE: ngưỡng checks rate (vd 1 để yêu cầu không có 5xx ngoài 429, Story 6.3)
// - THRESHOLD_ERROR_RATE: ngưỡng error_rate (vd 0.01 cho <1%, Story 6.4)

const BASE_URL = __ENV.BASE_URL || 'http://flashsale-api:8080';
const PRODUCT_ID = __ENV.PRODUCT_ID || 'flashsale-product-1';
const QUANTITY = Number(__ENV.QUANTITY || 1);
const EXECUTOR = __ENV.EXECUTOR || 'constant-vus';
const VUS = Number(__ENV.VUS || 8000);
const DURATION = __ENV.DURATION || '30s';
const RATE = Number(__ENV.RATE || 15000);
const TIME_UNIT = __ENV.TIME_UNIT || '1s';
const PREALLOCATED_VUS = Number(__ENV.PREALLOCATED_VUS || 2000);
const MAX_VUS = Number(__ENV.MAX_VUS || 15000);
const SLEEP_SECONDS = Number(__ENV.SLEEP_SECONDS || 0.1);
const THRESHOLD_P99_MS = __ENV.THRESHOLD_P99_MS ? Number(__ENV.THRESHOLD_P99_MS) : null;
const THRESHOLD_HTTP_REQ_FAILED_RATE = __ENV.THRESHOLD_HTTP_REQ_FAILED_RATE
  ? Number(__ENV.THRESHOLD_HTTP_REQ_FAILED_RATE)
  : 1;
const THRESHOLD_CHECKS_RATE = __ENV.THRESHOLD_CHECKS_RATE ? Number(__ENV.THRESHOLD_CHECKS_RATE) : null;
const THRESHOLD_ERROR_RATE = __ENV.THRESHOLD_ERROR_RATE ? Number(__ENV.THRESHOLD_ERROR_RATE) : null;

const thresholds = {
  http_req_failed: [`rate<${THRESHOLD_HTTP_REQ_FAILED_RATE}`],
};

// Story 6.2 (NFR-01): p99 latency luồng nhận đơn < 20ms (fast-path).
if (THRESHOLD_P99_MS !== null && !Number.isNaN(THRESHOLD_P99_MS)) {
  thresholds.http_req_duration = [`p(99)<${THRESHOLD_P99_MS}`];
}

// Story 6.3 (NFR-02): không chấp nhận 5xx do quá tải (ngoại 429) -> mọi response phải thuộc tập cho phép.
if (THRESHOLD_CHECKS_RATE !== null && !Number.isNaN(THRESHOLD_CHECKS_RATE)) {
  thresholds.checks = [`rate>=${THRESHOLD_CHECKS_RATE}`];
}

// Story 6.4 (NFR-03): Error Rate < 1% (5xx/timeout/response bất thường).
if (THRESHOLD_ERROR_RATE !== null && !Number.isNaN(THRESHOLD_ERROR_RATE)) {
  thresholds.error_rate = [`rate<${THRESHOLD_ERROR_RATE}`];
}

let scenario;
if (EXECUTOR === 'constant-arrival-rate') {
  scenario = {
    executor: 'constant-arrival-rate',
    rate: RATE,
    timeUnit: TIME_UNIT,
    duration: DURATION,
    preAllocatedVUs: PREALLOCATED_VUS,
    maxVUs: MAX_VUS,
  };
} else if (EXECUTOR === 'ramping-arrival-rate') {
  scenario = {
    executor: 'ramping-arrival-rate',
    startRate: Number(__ENV.START_RATE || 0),
    timeUnit: TIME_UNIT,
    preAllocatedVUs: PREALLOCATED_VUS,
    maxVUs: MAX_VUS,
    stages: [
      { target: RATE, duration: __ENV.RAMP_UP_DURATION || '10s' },
      { target: RATE, duration: __ENV.HOLD_DURATION || DURATION },
      { target: Number(__ENV.END_RATE || 0), duration: __ENV.RAMP_DOWN_DURATION || '10s' },
    ],
  };
} else {
  scenario = {
    executor: 'constant-vus',
    vus: VUS,
    duration: DURATION,
  };
}

export const options = {
  scenarios: {
    flash_sale_buy: scenario,
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
  // 5xx (ngoại 429) hoặc lỗi mạng/timeout được tính vào error_rate (Story 6.4).
  const okStatus = [202, 400, 401, 429].includes(res.status);
  check(res, {
    'status is expected (202/400/401/429)': () => okStatus,
  });
  errorRate.add(okStatus ? 0 : 1);

  if (SLEEP_SECONDS > 0) {
    sleep(SLEEP_SECONDS);
  }
}
