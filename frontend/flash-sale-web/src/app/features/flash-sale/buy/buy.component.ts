import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { FlashSaleService } from './flash-sale.service';
import {
  ApiErrorResponse,
  ConfirmHoldResponse,
  HoldResponse,
} from './buy.models';
import { UiFormFieldComponent } from '../../../shared/ui/form-field/ui-form-field.component';

type ResultType = 'success' | 'error' | 'warning' | 'info';
type HoldUiState = 'idle' | 'holding' | 'held' | 'confirming' | 'releasing' | 'success' | 'out_of_stock' | 'expired';

interface BuyResult {
  type: ResultType;
  message: string;
  orderId?: string;
  holdId?: string;
}

@Component({
  selector: 'app-buy',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, UiFormFieldComponent],
  templateUrl: './buy.component.html',
  styleUrl: './buy.component.scss',
})
export class BuyComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly flashSaleService = inject(FlashSaleService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    productId: ['', [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]],
  });

  readonly customerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required]],
    address: ['', [Validators.required]],
  });

  readonly loading = signal(false);
  readonly holdState = signal<HoldUiState>('idle');
  readonly result = signal<BuyResult | null>(null);
  readonly formError = signal<string | null>(null);
  readonly customerFormError = signal<string | null>(null);
  readonly activeHoldId = signal<string | null>(null);
  readonly holdExpiresAt = signal<string | null>(null);
  readonly remainingSeconds = signal(0);

  private countdownTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const productId = qp.get('productId');
    const quantity = qp.get('quantity');
    this.form.patchValue({
      productId: productId ?? this.form.controls.productId.value,
      quantity: quantity ? Number(quantity) : this.form.controls.quantity.value,
    });
  }

  ngOnDestroy(): void {
    this.clearCountdown();
  }

  createHold(): void {
    this.formError.set(null);
    this.customerFormError.set(null);
    this.result.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Vui lòng kiểm tra thông tin mua hàng trước khi gửi yêu cầu.');
      return;
    }

    const { productId, quantity } = this.form.getRawValue();
    this.loading.set(true);
    this.holdState.set('holding');

    this.flashSaleService
      .createHold({ productId: productId.trim(), quantity })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (res) => this.handleHoldSuccess(res),
        error: (err) => this.handleHoldError(err),
      });
  }

  confirmHold(): void {
    this.customerFormError.set(null);
    this.result.set(null);

    const holdId = this.activeHoldId();
    if (!holdId || this.holdState() !== 'held' || this.remainingSeconds() <= 0) {
      this.markExpired('Giữ chỗ đã hết hạn (quá 1 phút). Vui lòng giữ chỗ lại.');
      return;
    }

    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      this.customerFormError.set('Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ.');
      return;
    }

    const { name, phoneNumber, address } = this.customerForm.getRawValue();
    this.loading.set(true);
    this.holdState.set('confirming');

    this.flashSaleService
      .confirmHold(holdId, {
        name: name.trim(),
        phoneNumber: phoneNumber.trim(),
        address: address.trim(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (res) => this.handleConfirmSuccess(res),
        error: (err) => this.handleConfirmError(err),
      });
  }

  releaseHold(): void {
    const holdId = this.activeHoldId();
    if (!holdId) {
      this.resetToIdle();
      return;
    }

    this.loading.set(true);
    this.holdState.set('releasing');
    this.flashSaleService
      .releaseHold(holdId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.clearCountdown();
          this.resetToIdle('Đã hủy giữ chỗ. Bạn có thể giữ chỗ lại nếu sản phẩm còn hàng.');
        },
        error: (err) => {
          const httpError = err as HttpErrorResponse;
          if (httpError?.status === 401) {
            this.handleUnauthorized();
            return;
          }
          this.clearCountdown();
          this.resetToIdle('Giữ chỗ đã kết thúc. Bạn có thể giữ chỗ lại nếu sản phẩm còn hàng.');
        },
      });
  }

  resetForNewHold(): void {
    this.clearCountdown();
    this.activeHoldId.set(null);
    this.holdExpiresAt.set(null);
    this.remainingSeconds.set(0);
    this.customerForm.reset({ name: '', phoneNumber: '', address: '' });
    this.customerFormError.set(null);
    this.holdState.set('idle');
    this.result.set(null);
  }

  logout(): void {
    this.clearCountdown();
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  private handleHoldSuccess(res: HoldResponse): void {
    this.activeHoldId.set(res.holdId);
    this.holdExpiresAt.set(res.expiresAt);
    this.holdState.set('held');
    this.result.set({
      type: 'info',
      message: 'Đã giữ chỗ thành công. Vui lòng nhập thông tin và xác nhận trong 1 phút.',
      holdId: res.holdId,
    });
    this.startCountdown(res.expiresAt, res.ttlSeconds);
  }

  private handleHoldError(err: unknown): void {
    const httpError = err as HttpErrorResponse;
    const status = httpError?.status;
    const body = (httpError?.error ?? {}) as ApiErrorResponse;
    const code = this.getErrorCode(body);

    if (status === 401) {
      this.handleUnauthorized();
      return;
    }

    if (status === 429 || code === 'RATE_LIMITED' || code === 'RATE_LIMIT_EXCEEDED') {
      this.holdState.set('idle');
      this.result.set({
        type: 'warning',
        message: 'Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau.',
      });
      return;
    }

    if (status === 400 && code === 'INSUFFICIENT_INVENTORY') {
      this.holdState.set('out_of_stock');
      this.result.set({
        type: 'error',
        message: 'Sản phẩm đã hết hàng. Vui lòng thử lại sau.',
      });
      return;
    }

    if (status === 409 && code === 'HOLD_ALREADY_EXISTS') {
      this.holdState.set('idle');
      this.result.set({
        type: 'warning',
        message: 'Bạn đang có một giữ chỗ đang hoạt động. Vui lòng hoàn tất hoặc hủy giữ chỗ hiện tại.',
      });
      return;
    }

    if (status >= 500) {
      this.holdState.set('idle');
      this.result.set({ type: 'error', message: 'Có lỗi hệ thống. Vui lòng thử lại sau.' });
      return;
    }

    this.holdState.set('idle');
    this.result.set({
      type: 'error',
      message:
        status === 400
          ? (this.extractMessage(body) ?? 'Thông tin giữ chỗ không hợp lệ. Vui lòng kiểm tra lại.')
          : (this.extractMessage(body) ?? 'Có lỗi hệ thống. Vui lòng thử lại sau.'),
    });
  }

  private handleConfirmSuccess(res: ConfirmHoldResponse): void {
    this.clearCountdown();
    this.activeHoldId.set(null);
    this.holdExpiresAt.set(null);
    this.holdState.set('success');
    this.result.set({
      type: 'success',
      message: 'Đơn hàng đã được nhận và đang được xử lý.',
      orderId: res?.orderId as string | undefined,
      holdId: res?.holdId as string | undefined,
    });
  }

  private handleConfirmError(err: unknown): void {
    const httpError = err as HttpErrorResponse;
    const status = httpError?.status;
    const body = (httpError?.error ?? {}) as ApiErrorResponse;
    const code = this.getErrorCode(body);

    if (status === 401) {
      this.handleUnauthorized();
      return;
    }

    if (status === 429 || code === 'RATE_LIMITED' || code === 'RATE_LIMIT_EXCEEDED') {
      this.holdState.set('held');
      this.result.set({
        type: 'warning',
        message: 'Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau.',
      });
      return;
    }

    if (status === 410 || code === 'HOLD_EXPIRED') {
      this.markExpired('Giữ chỗ đã hết hạn (quá 1 phút). Vui lòng giữ chỗ lại.');
      return;
    }

    if (status === 409 || code === 'HOLD_ALREADY_CONFIRMED' || code === 'HOLD_CONSUMED' || code === 'HOLD_INVALID') {
      this.clearCountdown();
      this.activeHoldId.set(null);
      this.holdState.set(code === 'HOLD_INVALID' ? 'idle' : 'expired');
      this.result.set({
        type: 'warning',
        message:
          code === 'HOLD_INVALID'
            ? 'Giữ chỗ không hợp lệ. Vui lòng giữ chỗ lại.'
            : 'Giữ chỗ đã được xác nhận hoặc không còn hợp lệ. Vui lòng kiểm tra lại đơn hàng hoặc giữ chỗ mới.',
      });
      return;
    }

    if (status === 403 || code === 'HOLD_NOT_OWNED') {
      this.holdState.set('held');
      this.result.set({
        type: 'error',
        message: 'Bạn không có quyền thao tác giữ chỗ này.',
      });
      return;
    }

    if (status === 400) {
      this.holdState.set('held');
      this.result.set({
        type: 'error',
        message: this.extractMessage(body) ?? 'Thông tin xác nhận không hợp lệ. Vui lòng kiểm tra lại.',
      });
      return;
    }

    if (status >= 500) {
      this.holdState.set('held');
      this.result.set({ type: 'error', message: 'Có lỗi hệ thống. Vui lòng thử lại sau.' });
      return;
    }

    this.holdState.set('held');
    this.result.set({
      type: 'error',
      message: this.extractMessage(body) ?? 'Có lỗi hệ thống. Vui lòng thử lại sau.',
    });
  }

  private handleUnauthorized(): void {
    this.clearCountdown();
    this.logout();
  }

  private markExpired(message: string): void {
    this.clearCountdown();
    this.activeHoldId.set(null);
    this.holdExpiresAt.set(null);
    this.remainingSeconds.set(0);
    this.holdState.set('expired');
    this.result.set({ type: 'warning', message });
  }

  private resetToIdle(infoMessage?: string): void {
    this.activeHoldId.set(null);
    this.holdExpiresAt.set(null);
    this.remainingSeconds.set(0);
    this.customerForm.reset({ name: '', phoneNumber: '', address: '' });
    this.holdState.set('idle');
    this.result.set(infoMessage ? { type: 'info', message: infoMessage } : null);
  }

  private startCountdown(expiresAt?: string, ttlSeconds?: number): void {
    this.clearCountdown();
    const update = () => {
      const remainingMs = this.computeRemainingMs(expiresAt, ttlSeconds);
      const seconds = Math.max(0, Math.ceil(remainingMs / 1000));
      this.remainingSeconds.set(seconds);
      if (seconds <= 0 && this.holdState() === 'held') {
        this.markExpired('Giữ chỗ đã hết hạn (quá 1 phút). Vui lòng giữ chỗ lại.');
      }
    };

    update();
    this.countdownTimer = setInterval(update, 1000);
  }

  private computeRemainingMs(expiresAt?: string, _ttlSeconds?: number): number {
    if (expiresAt) {
      const target = new Date(expiresAt).getTime();
      if (!Number.isNaN(target)) {
        return target - Date.now();
      }
    }
    return 0;
  }

  private clearCountdown(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }

  private getErrorCode(body: ApiErrorResponse): string | undefined {
    const raw = body?.code ?? body?.status ?? body?.error;
    return typeof raw === 'string' ? raw : undefined;
  }

  private extractMessage(body: ApiErrorResponse): string | undefined {
    if (typeof body?.message === 'string' && body.message.trim()) return body.message;
    if (typeof body?.error === 'string' && body.error.trim()) return body.error;
    if (typeof body?.title === 'string' && body.title.trim()) return body.title;
    if (body?.errors && typeof body.errors === 'object') {
      const values = Object.values(body.errors).flat();
      const first = values.find((v) => typeof v === 'string' && v.trim());
      if (typeof first === 'string') return first;
    }
    return undefined;
  }
}