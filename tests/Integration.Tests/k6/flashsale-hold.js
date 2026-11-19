import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Story 8.8: k6 scenarios for Hold/Reservation flow (theo phạm vi khả thi).
// Scenarios:
// - MODE=confirm (default): hold -> confirm (còn hàng/giữ chỗ/confirm). Cần inventory đủ; confirm tạo Order qua pipeline.
// - MODE=hold_only: chỉ tạo hold, không confirm (phục vụ kiểm tra hết hạn nhả slot bởi WorkerUnHold; cần quan sát inventory/Redis/worker).
// - MODE=out_of_stock_probe: gọi hold và kỳ vọng có thể nhận INSUFFICIENT_INVENTORY khi hết hàng (kịch bản hết hàng).
//
// Lưu ý: 1 user/1 active hold. Với MODE=hold_only hoặc tải cao bằng 1 token, có thể gặp HOLD_ALREADY_EXISTS (409) - được xem là response hợp lệ.
// "Hết hạn nhả slot" cần WorkerUnHold chạy; script không tự verify slot được trả mà tạo điều kiện (hold không confirm).

const errorRate = new Rate('error_rate');

const BASE_URL = __ENV.BASE_URL || 'http://flashsale-api:8080';
const PRODUCT_ID = __ENV.PRODUCT_ID || 'flashsale-product-1';
const QUANTITY = Number(__ENV.QUANTITY || 1);
const MODE = __ENV.MODE || 'confirm'; // confirm | hold_only | out_of_stock_probe
const EXECUTOR = __ENV.EXECUTOR || 'constant-vus';
const VUS = Number(__ENV.VUS || 10);
const DURATION = __ENV.DURATION || '30s';
const RATE = Number(__ENV.RATE || 100);
const TIME_UNIT = __ENV.TIME_UNIT || '1s';
const PREALLOCATED_VUS = Number(__ENV.PREALLOCATED_VUS || 20);
const MAX_VUS = Number(__ENV.MAX_VUS || 200);
const SLEEP_SECONDS = Number(__ENV.SLEEP_SECONDS || 0.1);
const THINK_TIME_SECONDS = Number(__ENV.THINK_TIME_SECONDS || 0.2);
const THRESHOLD_HTTP_REQ_FAILED_RATE = __ENV.THRESHOLD_HTTP_REQ_FAILED_RATE
  ? Number(__ENV.THRESHOLD_HTTP_REQ_FAILED_RATE)
  : 1;
const THRESHOLD_CHECKS_RATE = __ENV.THRESHOLD_CHECKS_RATE ? Number(__ENV.THRESHOLD_CHECKS_RATE) : null;
const THRESHOLD_ERROR_RATE = __ENV.THRESHOLD_ERROR_RATE ? Number(__ENV.THRESHOLD_ERROR_RATE) : null;

const thresholds = {
  http_req_failed: [`rate<${THRESHOLD_HTTP_REQ_FAILED_RATE}`],
};
if (THRESHOLD_CHECKS_RATE !== null && !Number.isNaN(THRESHOLD_CHECKS_RATE)) {
  thresholds.checks = [`rate>=${THRESHOLD_CHECKS_RATE}`];
}
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
} else {
  scenario = {
    executor: 'constant-vus',
    vus: VUS,
    duration: DURATION,
  };
}

export const options = {
  scenarios: {
    flash_sale_hold: scenario,
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

function authHeaders(data) {
  const token = (data && data.accessToken) || __ENV.ACCESS_TOKEN || __ENV.TOKEN;
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  return headers;
}

function parseJson(res) {
  try {
    return res.json();
  } catch (_) {
    return null;
  }
}

export default function (data) {
  const headers = authHeaders(data);
  const holdPayload = JSON.stringify({ productId: PRODUCT_ID, quantity: QUANTITY });
  const holdRes = http.post(`${BASE_URL}/api/v1/flash-sale/hold`, holdPayload, { headers });

  const holdOkStatus = [200, 201, 400, 401, 409, 429].includes(holdRes.status);
  const holdBody = parseJson(holdRes);
  const holdCode = holdBody && holdBody.code ? holdBody.code : null;
  const holdId = holdBody && holdBody.holdId ? holdBody.holdId : null;

  // Kịch bản hợp lệ: giữ chỗ thành công, hết hàng, đã có hold active, rate limit, unauthorized
  const holdExpected =
    (holdRes.status === 200 || holdRes.status === 201) ||
    (holdRes.status === 400 && holdCode === 'INSUFFICIENT_INVENTORY') ||
    holdRes.status === 401 ||
    holdRes.status === 429 ||
    (holdRes.status === 409 && (holdCode === 'HOLD_ALREADY_EXISTS' || holdCode));

  check(holdRes, {
    'hold: status expected': () => holdOkStatus,
    'hold: response expected for scenario': () => holdExpected,
  });

  let unexpected = !(holdOkStatus && holdExpected);

  if ((holdRes.status === 200 || holdRes.status === 201) && holdId && MODE === 'confirm') {
    if (THINK_TIME_SECONDS > 0) sleep(THINK_TIME_SECONDS);

    const confirmPayload = JSON.stringify({
      name: `K6 User ${__VU}`,
      phoneNumber: '0900000000',
      address: `Address VU ${__VU} ITER ${__ITER}`,
    });
    const confirmRes = http.post(
      `${BASE_URL}/api/v1/flash-sale/hold/${holdId}/confirm`,
      confirmPayload,
      { headers }
    );
    const confirmBody = parseJson(confirmRes);
    const confirmCode = confirmBody && confirmBody.code ? confirmBody.code : null;
    const confirmOkStatus = [202, 400, 401, 403, 409, 410, 429].includes(confirmRes.status);
    const confirmExpected =
      confirmRes.status === 202 ||
      confirmRes.status === 401 ||
      confirmRes.status === 429 ||
      confirmRes.status === 410 ||
      confirmRes.status === 403 ||
      (confirmRes.status === 409 && confirmCode) ||
      (confirmRes.status === 400 && confirmCode);

    check(confirmRes, {
      'confirm: status expected': () => confirmOkStatus,
      'confirm: response expected for scenario': () => confirmExpected,
    });

    if (!(confirmOkStatus && confirmExpected)) unexpected = true;
  }

  // MODE=hold_only: không confirm để hold hết hạn và WorkerUnHold nhả slot (cần worker chạy, verify ngoài script).
  // MODE=out_of_stock_probe: chỉ quan sát hold response (INSUFFICIENT_INVENTORY khi hết hàng).

  errorRate.add(unexpected ? 1 : 0);

  if (SLEEP_SECONDS > 0) {
    sleep(SLEEP_SECONDS);
  }
}