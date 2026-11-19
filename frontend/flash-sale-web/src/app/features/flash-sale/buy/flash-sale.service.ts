import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BuyRequest,
  BuyResponse,
  ConfirmHoldRequest,
  ConfirmHoldResponse,
  HoldRequest,
  HoldResponse,
  ReleaseHoldResponse,
} from './buy.models';

@Injectable({ providedIn: 'root' })
export class FlashSaleService {
  constructor(private readonly http: HttpClient) {}

  buy(request: BuyRequest): Observable<BuyResponse> {
    return this.http.post<BuyResponse>('/api/v1/flash-sale/buy', request);
  }

  createHold(request: HoldRequest): Observable<HoldResponse> {
    return this.http.post<HoldResponse>('/api/v1/flash-sale/hold', request);
  }

  confirmHold(holdId: string, request: ConfirmHoldRequest): Observable<ConfirmHoldResponse> {
    return this.http.post<ConfirmHoldResponse>(`/api/v1/flash-sale/hold/${holdId}/confirm`, request);
  }

  releaseHold(holdId: string): Observable<ReleaseHoldResponse> {
    return this.http.post<ReleaseHoldResponse>(`/api/v1/flash-sale/hold/${holdId}/release`, {});
  }
}
