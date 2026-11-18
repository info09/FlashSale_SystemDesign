import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BuyComponent } from './buy.component';
import { authInterceptor } from '../../../core/auth/auth.interceptor';

describe('Buy flow (UI + API integration)', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
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
  });

  it('should submit buy request to API and show 202 accepted result', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 2 });
    component.submit();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/flash-sale/buy');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'product-test', quantity: 2 });
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');

    req.flush({ orderId: 'order-123', message: 'accepted' }, { status: 202, statusText: 'Accepted' });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.result()?.type).toBe('success');
    expect(component.result()?.message).toContain('Đơn hàng đã được nhận và đang được xử lý.');
    expect(component.result()?.orderId).toBe('order-123');

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Đơn hàng đã được nhận và đang được xử lý.');
    expect(el.textContent).toContain('order-123');
  });

  it('should show RateLimit message when API returns 429', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 1 });
    component.submit();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/flash-sale/buy');
    req.flush({ code: 'RATE_LIMITED' }, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.result()?.type).toBe('warning');
    expect(component.result()?.message).toContain('Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau.');
  });

  it('should show insufficient Inventory message when API returns 400', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'product-test', quantity: 1 });
    component.submit();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/flash-sale/buy');
    req.flush({ code: 'INSUFFICIENT_INVENTORY', message: 'Hết hàng' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.result()?.type).toBe('error');
    expect(component.result()?.message).toContain('Sản phẩm đã hết hàng. Đơn hàng không được tạo.');
  });

  it('should logout and navigate to login on 401', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({ productId: 'product-test', quantity: 1 });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/flash-sale/buy');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(localStorage.getItem('flashsale_access_token')).toBeNull();
    expect(navSpy).toHaveBeenCalled();
  });
});
