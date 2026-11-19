import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { BuyComponent } from './buy.component';
import { AuthService } from '../../../core/auth/auth.service';
import { FlashSaleService } from './flash-sale.service';

async function flushMicrotasks(fixture: { detectChanges: () => void }): Promise<void> {
  fixture.detectChanges();
  await Promise.resolve();
  fixture.detectChanges();
}

describe('BuyComponent (Hold flow)', () => {
  let flashSaleServiceMock: {
    createHold: ReturnType<typeof vi.fn>;
    confirmHold: ReturnType<typeof vi.fn>;
    releaseHold: ReturnType<typeof vi.fn>;
  };
  let authServiceMock: { logout: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    flashSaleServiceMock = {
      createHold: vi.fn(),
      confirmHold: vi.fn(),
      releaseHold: vi.fn(),
    };
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

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(BuyComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should show validation error when hold form invalid', async () => {
    const fixture = TestBed.createComponent(BuyComponent);
    fixture.componentInstance.form.setValue({ productId: '', quantity: 0 } as any);
    fixture.componentInstance.createHold();
    await flushMicrotasks(fixture);

    expect(fixture.componentInstance.formError()).toContain('Vui lòng kiểm tra');
    expect(flashSaleServiceMock.createHold).not.toHaveBeenCalled();
  });

  it('should create hold successfully and show customer form with countdown', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:01:00Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 60, code: 'HOLD_CREATED' }),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('held');
    expect(component.activeHoldId()).toBe('hold-1');
    expect(component.remainingSeconds()).toBe(60);

    vi.advanceTimersByTime(1000);
    fixture.detectChanges();
    expect(component.remainingSeconds()).toBe(59);

    fixture.destroy();
  });

  it('should show out of stock message when hold returns INSUFFICIENT_INVENTORY', async () => {
    flashSaleServiceMock.createHold.mockReturnValue(
      throwError(() => ({ status: 400, error: { code: 'INSUFFICIENT_INVENTORY' } })),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('out_of_stock');
    expect(component.result()?.type).toBe('error');
    expect(component.result()?.message).toContain('Sản phẩm đã hết hàng');
  });

  it('should handle 429 RateLimit when creating hold', async () => {
    flashSaleServiceMock.createHold.mockReturnValue(throwError(() => ({ status: 429, error: {} })));

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    expect(component.result()?.type).toBe('warning');
    expect(component.result()?.message).toContain('Bạn đã gửi quá nhiều yêu cầu');
  });

  it('should logout and navigate on 401 when creating hold', async () => {
    flashSaleServiceMock.createHold.mockReturnValue(throwError(() => ({ status: 401, error: {} })));
    const fixture = TestBed.createComponent(BuyComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance.form.setValue({ productId: 'p1', quantity: 1 });
    fixture.componentInstance.createHold();
    await flushMicrotasks(fixture);

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalled();
  });

  it('should confirm hold successfully and show success result', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:01:00Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 60 }),
    );
    flashSaleServiceMock.confirmHold.mockReturnValue(
      of({ code: 'ORDER_ACCEPTED', orderId: 'order-1', holdId: 'hold-1', correlationId: 'corr-1' }),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    component.customerForm.setValue({ name: 'Nguyen Van A', phoneNumber: '0900000000', address: 'Ha Noi' });
    component.confirmHold();
    await flushMicrotasks(fixture);

    expect(flashSaleServiceMock.confirmHold).toHaveBeenCalledWith('hold-1', {
      name: 'Nguyen Van A',
      phoneNumber: '0900000000',
      address: 'Ha Noi',
    });
    expect(component.holdState()).toBe('success');
    expect(component.result()?.type).toBe('success');
    expect(component.result()?.message).toContain('Đơn hàng đã được nhận và đang được xử lý.');
    expect(component.result()?.orderId).toBe('order-1');

    fixture.destroy();
  });

  it('should require customer info before confirm', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:01:00Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 60 }),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    component.customerForm.setValue({ name: '', phoneNumber: '', address: '' });
    component.confirmHold();
    fixture.detectChanges();

    expect(flashSaleServiceMock.confirmHold).not.toHaveBeenCalled();
    expect(component.customerFormError()).toBeTruthy();

    fixture.destroy();
  });

  it('should mark hold as expired when countdown reaches zero', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:00:02Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 2 }),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    vi.advanceTimersByTime(2000);
    fixture.detectChanges();

    expect(component.holdState()).toBe('expired');
    expect(component.result()?.message).toContain('Giữ chỗ đã hết hạn');

    fixture.destroy();
  });

  it('should handle confirm expired (410)', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:01:00Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 60 }),
    );
    flashSaleServiceMock.confirmHold.mockReturnValue(
      throwError(() => ({ status: 410, error: { code: 'HOLD_EXPIRED' } })),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    component.customerForm.setValue({ name: 'A', phoneNumber: '0900', address: 'HN' });
    component.confirmHold();
    await flushMicrotasks(fixture);

    expect(component.holdState()).toBe('expired');
    expect(component.result()?.message).toContain('Giữ chỗ đã hết hạn');

    fixture.destroy();
  });

  it('should handle confirm conflict (409)', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:01:00Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 60 }),
    );
    flashSaleServiceMock.confirmHold.mockReturnValue(
      throwError(() => ({ status: 409, error: { code: 'HOLD_ALREADY_CONFIRMED' } })),
    );

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    component.customerForm.setValue({ name: 'A', phoneNumber: '0900', address: 'HN' });
    component.confirmHold();
    await flushMicrotasks(fixture);

    expect(component.result()?.type).toBe('warning');
    expect(component.result()?.message).toContain('Giữ chỗ đã được xác nhận');

    fixture.destroy();
  });

  it('should release hold and return to idle state', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-19T10:00:00Z'));
    const expiresAt = new Date('2026-11-19T10:01:00Z').toISOString();
    flashSaleServiceMock.createHold.mockReturnValue(
      of({ holdId: 'hold-1', productId: 'p1', quantity: 1, expiresAt, ttlSeconds: 60 }),
    );
    flashSaleServiceMock.releaseHold.mockReturnValue(of({ code: 'HOLD_RELEASED', released: true }));

    const fixture = TestBed.createComponent(BuyComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ productId: 'p1', quantity: 1 });
    component.createHold();
    await flushMicrotasks(fixture);

    component.releaseHold();
    await flushMicrotasks(fixture);

    expect(flashSaleServiceMock.releaseHold).toHaveBeenCalledWith('hold-1');
    expect(component.holdState()).toBe('idle');
    expect(component.activeHoldId()).toBeNull();

    fixture.destroy();
  });
});
