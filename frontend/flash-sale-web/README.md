# FlashSale Web (Angular)

Frontend Angular cho Story 7.1: màn hình **Gọi API mua hàng** (FlashSaleShopper) với luồng đăng nhập lấy Bearer token.

## Yêu cầu

- Node.js + npm
- Backend FlashSale API chạy tại `http://localhost:8080` (hoặc chỉnh `proxy.conf.json`)

## Chạy development

```bash
npm install
npm start
```

- App: `http://localhost:4200`
- Proxy dev: `/api/*` -> `http://localhost:8080` (xem `proxy.conf.json`)

## Luồng sử dụng

1. Đăng nhập tại `/login` (`POST /api/v1/auth/login`) để nhận `accessToken`.
2. Mua hàng tại `/buy` (`POST /api/v1/flash-sale/buy`, Bearer token tự gắn qua interceptor).
3. Trạng thái hiển thị theo spec: 202 nhận đơn, 400 hết kho/invalid, 401 yêu cầu đăng nhập lại, 429 vượt RateLimit, 5xx/lỗi mạng.

## Build

```bash
npm run build
```

## Ghi chú

- Token lưu tại `localStorage` key `flashsale_access_token`.
- Thiết kế tham chiếu: `docs/ux-flashsale-buy-screen.md`.
