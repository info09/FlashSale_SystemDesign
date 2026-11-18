import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BuyRequest, BuyResponse } from './buy.models';

@Injectable({ providedIn: 'root' })
export class FlashSaleService {
  constructor(private readonly http: HttpClient) {}

  buy(request: BuyRequest): Observable<BuyResponse> {
    return this.http.post<BuyResponse>('/api/v1/flash-sale/buy', request);
  }
}
