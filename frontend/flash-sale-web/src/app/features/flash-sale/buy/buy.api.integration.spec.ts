import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BuyComponent } from './buy.component';
import { authInterceptor } from '../../../core/auth/auth.interceptor';

async function flushMicrotasks(fixture: { detectChanges: () => void }): Promise<void> {
  fixture.detectChanges();
  await Promise.resolve();
  fixture.detectChanges();
}

describe('Hold flow (UI + API integration)', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    localStorage.clear();
    localStorage.setItem('flashsale_access_token', 'test-token');

    await TestBed.configureTestingModule({
      imports: [BuyComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    vi.useRealTimers();
  });

  it('should create hold, show countdown/form, confirm and show 202 accepted result', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 2 });
    component.createHold();
    fixture.detectChanges();

    const holdReq = httpMock.expectOne('/api/v1/flash-sale/hold');
    expect(holdReq.request.method).toBe('POST');
    expect(holdReq.request.body).toEqual({ productId: 'product-test', quantity: 2 });
    expect(holdReq.request.headers.get('Authorization')).toBe('Bearer test-token');

    holdReq.flush({
      status: 'success',
      code: 'HOLD_CREATED',
      holdId: 'hold-123',
      productId: 'product-test',
      quantity: 2,
      expiresAt: '2026-11-19T10:01:00Z',
      ttlSeconds: 60,
    });
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('held');
    expect(component.activeHoldId()).toBe('hold-123');
    expect(component.remainingSeconds()).toBe(60);

    vi.advanceTimersByTime(1000);
    fixture.detectChanges();
    expect(component.remainingSeconds()).toBe(59);

    component.customerForm.setValue({ name: 'Nguyen Van A', phoneNumber: '0900000000', address: 'Ha Noi' });
    component.confirmHold();
    fixture.detectChanges();

    const confirmReq = httpMock.expectOne('/api/v1/flash-sale/hold/hold-123/confirm');
    expect(confirmReq.request.method).toBe('POST');
    expect(confirmReq.request.body).toEqual({ name: 'Nguyen Van A', phoneNumber: '0900000000', address: 'Ha Noi' });

    confirmReq.flush(
      {
        status: 'success',
        code: 'ORDER_ACCEPTED',
        message: 'Đơn hàng đã được nhận và đang được xử lý.',
        holdId: 'hold-123',
        orderId: 'order-123',
        correlationId: 'corr-123',
      },
      { status: 202, statusText: 'Accepted' },
    );
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('success');
    expect(component.result()?.type).toBe('success');
    expect(component.result()?.message).toContain('Đơn hàng đã được nhận và đang được xử lý.');
    expect(component.result()?.orderId).toBe('order-123');
  });

  it('should show out of stock when hold returns INSUFFICIENT_INVENTORY', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 1 });
    component.createHold();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/flash-sale/hold');
    req.flush({ code: 'INSUFFICIENT_INVENTORY', message: 'Hết hàng' }, { status: 400, statusText: 'Bad Request' });
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('out_of_stock');
    expect(component.result()?.type).toBe('error');
    expect(component.result()?.message).toContain('Sản phẩm đã hết hàng');
  });

  it('should mark hold expired when countdown reaches zero', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 1 });
    component.createHold();
    fixture.detectChanges();

    httpMock.expectOne('/api/v1/flash-sale/hold').flush({
      status: 'success',
      code: 'HOLD_CREATED',
      holdId: 'hold-exp',
      expiresAt: '2026-11-19T10:00:02Z',
      ttlSeconds: 2,
    });
    await flushMicrotasks(fixture);

    vi.advanceTimersByTime(2000);
    fixture.detectChanges();

    expect(component.holdState()).toBe('expired');
    expect(component.result()?.message).toContain('Giữ chỗ đã hết hạn');
  });

  it('should handle confirm failure when hold expired (410)', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 1 });
    component.createHold();
    fixture.detectChanges();

    httpMock.expectOne('/api/v1/flash-sale/hold').flush({
      status: 'success',
      code: 'HOLD_CREATED',
      holdId: 'hold-410',
      expiresAt: '2026-11-19T10:01:00Z',
      ttlSeconds: 60,
    });
    await flushMicrotasks(fixture);

    component.customerForm.setValue({ name: 'A', phoneNumber: '0900', address: 'HN' });
    component.confirmHold();
    fixture.detectChanges();

    const confirmReq = httpMock.expectOne('/api/v1/flash-sale/hold/hold-410/confirm');
    confirmReq.flush({ code: 'HOLD_EXPIRED', message: 'Hết hạn' }, { status: 410, statusText: 'Gone' });
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('expired');
    expect(component.result()?.message).toContain('Giữ chỗ đã hết hạn');
  });

  it('should logout and navigate to login on 401 when creating hold', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({ productId: 'product-test', quantity: 1 });
    fixture.componentInstance.createHold();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/flash-sale/hold');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    await flushMicrotasks(fixture);

    expect(localStorage.getItem('flashsale_access_token')).toBeNull();
    expect(navSpy).toHaveBeenCalled();
  });
});
