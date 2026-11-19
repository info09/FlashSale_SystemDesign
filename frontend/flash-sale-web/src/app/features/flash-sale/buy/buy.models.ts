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

export interface HoldRequest {
  productId: string;
  quantity: number;
}

export interface HoldResponse {
  status?: string;
  code?: string;
  holdId: string;
  productId?: string;
  quantity?: number;
  expiresAt: string;
  ttlSeconds?: number;
  message?: string;
  [key: string]: unknown;
}

export interface ConfirmHoldRequest {
  name: string;
  phoneNumber: string;
  address: string;
}

export interface ConfirmHoldResponse {
  status?: string;
  code?: string;
  message?: string;
  holdId?: string;
  orderId?: string;
  correlationId?: string;
  productId?: string;
  quantity?: number;
  [key: string]: unknown;
}

export interface ReleaseHoldResponse {
  status?: string;
  code?: string;
  holdId?: string;
  released?: boolean;
  message?: string;
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
