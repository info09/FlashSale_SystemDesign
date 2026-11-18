export interface LoginRequest {
  userName: string;
  password: string;
}

export interface LoginResponse {
  accessToken?: string;
  AccessToken?: string;
  token?: string;
  userName?: string;
  email?: string;
  expiresAt?: string;
  [key: string]: unknown;
}
