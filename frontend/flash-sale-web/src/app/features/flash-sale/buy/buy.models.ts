export interface BuyRequest {
  productId: string;
  quantity: number;
}

export interface BuyResponse {
  message?: string;
  orderId?: string;
  code?: string;
  [key: string]: unknown;
}

export interface ApiErrorResponse {
  message?: string;
  error?: string;
  code?: string;
  status?: string;
  title?: string;
  errors?: Record<string, string[] | string>;
  [key: string]: unknown;
}
