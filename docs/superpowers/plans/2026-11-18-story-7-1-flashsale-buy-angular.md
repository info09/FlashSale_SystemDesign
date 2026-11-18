# Story 7.1 FlashSale Buy Screen (Angular) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Triển khai màn hình Gọi API mua hàng (FlashSaleShopper) bằng Angular với luồng đăng nhập lấy Bearer token (phương án A), theo `docs/ux-flashsale-buy-screen.md`.

**Architecture:** Tạo Angular app tách biệt tại `frontend/flash-sale-web` (standalone components, routing, SCSS). `core` cung cấp `AuthService`, interceptor gắn token, guard; `features/auth/login` gọi `/api/v1/auth/login`; `features/flash-sale/buy` gọi `/api/v1/flash-sale/buy` và hiển thị trạng thái 202/400/401/429/5xx theo spec. Dev proxy chuyển `/api` tới backend.

**Tech Stack:** Angular (latest via CLI), TypeScript, SCSS, Angular Router, HttpClient, RxJS.

**Spec:** `docs/ux-flashsale-buy-screen.md`, `docs/ux.md` (IA, copy, states), `docs/decisions.md` ([2026-11-18] Angular), approved chat design (A: login flow).

## Global Constraints

- Thuật ngữ/nghiệp vụ hiển thị khớp `docs/domain-glossary.md` và copy trong `docs/ux-flashsale-buy-screen.md`/`docs/ux.md`.
- API endpoints hiện có: `POST /api/v1/auth/login`, `POST /api/v1/flash-sale/buy` (Bearer token).
- Không tự động commit. Xin phép trước khi chạy lệnh shell (scaffold, install, build, serve).
- Frontend ngoài phạm vi MVP backend; chỉ triển khai đủ Story 7.1 (login + buy).

## Review Focus

- Token hết hạn/401 khi mua: phải xóa token, báo lỗi/điều hướng login, không để trạng thái “thành công giả”.
- Phân biệt rõ `202 Accepted` (đơn đã nhận, đang xử lý) với hoàn tất xử lý; không hiển thị copy sai nghĩa.
- 429/400 hết kho/5xx hiển thị đúng thông điệp và cho phép thao tác lại hợp lý.
- Proxy/CORS dev: request `/api/...` phải tới đúng backend.

---

### Task 1: Scaffold Angular app và cấu hình dev

**Files:**
- Create: `frontend/flash-sale-web/**` (Angular workspace/app)
- Create/Modify: `frontend/flash-sale-web/proxy.conf.json`
- Modify: `frontend/flash-sale-web/angular.json` (serve proxy config)
- Modify: `frontend/flash-sale-web/src/app/app.config.ts`, `app.routes.ts`, `app.component.*`

**Interfaces:**
- Produces: app chạy được, route shell, HttpClient + router configured, proxy `/api`.

- [ ] **Step 1: Scaffold project**
  - Run (with permission): `npx -y @angular/cli@latest new flash-sale-web --directory frontend/flash-sale-web --routing --style=scss --ssr=false --skip-git --skip-tests=false`
  - Expected: project created under `frontend/flash-sale-web`.

- [ ] **Step 2: Add proxy config**
  - Create `proxy.conf.json` proxying `/api` to `http://localhost:8080` (changeOrigin, secure false).
  - Wire proxy in `angular.json` serve options (or npm script `start`).

- [ ] **Step 3: Verify app builds**
  - Run (with permission): `npm run build` in `frontend/flash-sale-web`.
  - Expected: build succeeds.

- [ ] **Step 4: Commit**
  - Skip (no auto commit).

### Task 2: Core auth (service, interceptor, guard, models)

**Files:**
- Create: `frontend/flash-sale-web/src/app/core/auth/auth.models.ts`
- Create: `frontend/flash-sale-web/src/app/core/auth/auth.service.ts`
- Create: `frontend/flash-sale-web/src/app/core/auth/auth.interceptor.ts`
- Create: `frontend/flash-sale-web/src/app/core/auth/auth.guard.ts`
- Modify: `frontend/flash-sale-web/src/app/app.config.ts`

**Interfaces:**
- Produces: `AuthService.login(userName,password)`, token storage key `flashsale_access_token`, `isAuthenticated()`, `logout()`, `getToken()`; functional interceptor adds Bearer token; `authGuard` protects routes.

- [ ] **Step 1: Implement auth models/service**
  - Login request/response types aligned to API (`accessToken` in response).

- [ ] **Step 2: Implement interceptor and guard**
  - Attach `Authorization` header when token exists; redirect to `/login` on guard failure.

- [ ] **Step 3: Register providers**
  - Provide HttpClient with interceptor, router guard usage.

- [ ] **Step 4: Verify build**

### Task 3: Login feature

**Files:**
- Create: `frontend/flash-sale-web/src/app/features/auth/login/login.component.ts|html|scss`
- Modify: `frontend/flash-sale-web/src/app/app.routes.ts`

**Interfaces:**
- Produces: `/login` route, reactive/template form (userName, password), loading/error states, success navigates to `/buy`.

- [ ] **Step 1: Implement Login component and template**
  - Validation, disable submit while loading, show API/401/network errors with clear Vietnamese copy.

- [ ] **Step 2: Wire route**
  - Default route redirects to `/buy`; `/buy` guarded; `/login` public.

- [ ] **Step 3: Verify build**

### Task 4: Flash sale buy feature (Story 7.1)

**Files:**
- Create: `frontend/flash-sale-web/src/app/features/flash-sale/buy/buy.models.ts`
- Create: `frontend/flash-sale-web/src/app/features/flash-sale/buy/flash-sale.service.ts`
- Create: `frontend/flash-sale-web/src/app/features/flash-sale/buy/buy.component.ts|html|scss`
- Modify: `frontend/flash-sale-web/src/app/app.routes.ts`

**Interfaces:**
- Produces: `/buy` protected route, form `productId`, `quantity`; `FlashSaleService.buy()` posts to `/api/v1/flash-sale/buy`; UI states/copy per spec (202 received-processing with orderId, 400 insufficient inventory/invalid, 401 -> logout+login, 429 rate limit, 5xx/network error), empty/loading feedback.

- [ ] **Step 1: Implement service and models**
  - Typed request/response/error handling.

- [ ] **Step 2: Implement Buy component UI and state handling**
  - Follow `docs/ux-flashsale-buy-screen.md` states and copy; prevent double submit while loading.

- [ ] **Step 3: Wire route and navigation**
  - Ensure guard + redirect flow correct.

- [ ] **Step 4: Verify build**

### Task 5: Polish, docs/status

**Files:**
- Modify: `frontend/flash-sale-web/README.md` (run instructions, proxy, API base)
- Modify: `docs/status.md` (ghi nhận triển khai Angular cho 7.1 nếu hoàn tất)

**Interfaces:**
- Produces: runnable instructions; status reflects implementation progress.

- [ ] **Step 1: Update README and status**
- [ ] **Step 2: Final build verification**
  - Run (with permission): `npm run build`.

## Execution Handoff

Plan saved. Đại ca đã duyệt design (phương án A). Em sẽ thực thi trực tiếp (native) trong session này theo các task, xin phép trước mỗi lệnh shell. Nếu Đại ca muốn đổi sang cách khác, báo em.
