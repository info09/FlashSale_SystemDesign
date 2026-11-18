import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { BuyComponent } from './buy.component';
import { AuthService } from '../../../core/auth/auth.service';
import { FlashSaleService } from './flash-sale.service';

describe('BuyComponent', () => {
  let flashSaleServiceMock: { buy: ReturnType<typeof vi.fn> };
  let authServiceMock: { logout: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    flashSaleServiceMock = { buy: vi.fn() };
    authServiceMock = { logout: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [BuyComponent],
      providers: [
        provideRouter([]),
        { provide: FlashSaleService, useValue: flashSaleServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
      ],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(BuyComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should show validation error when form invalid', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.componentInstance.form.setValue({ productId: '', quantity: 0 } as any);
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.formError()).toContain('Vui lòng kiểm tra');
    expect(flashSaleServiceMock.buy).not.toHaveBeenCalled();
  });

  it('should handle 202 accepted success message', async () => {
    flashSaleServiceMock.buy.mockReturnValue(of({ orderId: 'order-1' }));
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.componentInstance.form.setValue({ productId: 'p1', quantity: 1 });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.result()?.type).toBe('success');
    expect(fixture.componentInstance.result()?.message).toContain('Đơn hàng đã được nhận và đang được xử lý.');
    expect(fixture.componentInstance.result()?.orderId).toBe('order-1');
  });

  it('should handle 429 RateLimit message', async () => {
    flashSaleServiceMock.buy.mockReturnValue(throwError(() => ({ status: 429, error: {} })));
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.componentInstance.form.setValue({ productId: 'p1', quantity: 1 });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.result()?.type).toBe('warning');
    expect(fixture.componentInstance.result()?.message).toContain('Bạn đã gửi quá nhiều yêu cầu');
  });

  it('should handle insufficient inventory (400)', async () => {
    flashSaleServiceMock.buy.mockReturnValue(throwError(() => ({ status: 400, error: { code: 'INSUFFICIENT_INVENTORY' } })));
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.componentInstance.form.setValue({ productId: 'p1', quantity: 1 });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.result()?.type).toBe('error');
    expect(fixture.componentInstance.result()?.message).toContain('Sản phẩm đã hết hàng. Đơn hàng không được tạo.');
  });

  it('should logout and navigate on 401', async () => {
    flashSaleServiceMock.buy.mockReturnValue(throwError(() => ({ status: 401, error: {} })));
    const fixture = TestBed.createComponent(BuyComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance.form.setValue({ productId: 'p1', quantity: 1 });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalled();
  });
});
