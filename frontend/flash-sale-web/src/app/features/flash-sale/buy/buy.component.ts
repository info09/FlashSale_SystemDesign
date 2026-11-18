import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { FlashSaleService } from './flash-sale.service';
import { ApiErrorResponse, BuyResponse } from './buy.models';

type ResultType = 'success' | 'error' | 'warning' | 'info';

interface BuyResult {
  type: ResultType;
  message: string;
  orderId?: string;
}

@Component({
  selector: 'app-buy',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './buy.component.html',
  styleUrl: './buy.component.scss'
})
export class BuyComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly flashSaleService = inject(FlashSaleService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly result = signal<BuyResult | null>(null);
  readonly formError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    productId: ['product-test', [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('reason') === 'unauthorized') {
      this.result.set({
        type: 'error',
        message: 'Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.'
      });
    }
  }

  submit(): void {
    this.formError.set(null);
    this.result.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Vui lòng kiểm tra thông tin mua hàng.');
      return;
    }

    const { productId, quantity } = this.form.getRawValue();
    this.loading.set(true);
    this.flashSaleService
      .buy({ productId: productId.trim(), quantity: Number(quantity) })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => this.handleSuccess(response),
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  reset(): void {
    this.form.reset({ productId: 'product-test', quantity: 1 });
    this.formError.set(null);
    this.result.set(null);
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  private handleSuccess(response: BuyResponse): void {
    this.result.set({
      type: 'success',
      message: 'Đơn hàng đã được nhận và đang được xử lý.',
      orderId: response.orderId
    });
  }

  private handleError(error: HttpErrorResponse): void {
    const status = error.status;
    const body = (error.error || {}) as ApiErrorResponse;
    const code = (body.code || '').toString().toUpperCase();
    const message = (body.message || body.error || '').toString();

    if (status === 401) {
      this.authService.logout();
      void this.router.navigate(['/login'], { queryParams: { reason: 'unauthorized' } });
      return;
    }

    if (status === 429) {
      this.result.set({
        type: 'warning',
        message: 'Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi và thử lại sau.'
      });
      return;
    }

    if (status === 400) {
      if (code === 'INSUFFICIENT_INVENTORY' || /hết hàng|insufficient/i.test(message)) {
        this.result.set({
          type: 'error',
          message: 'Sản phẩm đã hết hàng. Đơn hàng không được tạo.'
        });
        return;
      }
      this.result.set({
        type: 'error',
        message: message || 'Yêu cầu không hợp lệ. Vui lòng kiểm tra ProductId và Quantity.'
      });
      return;
    }

    if (status === 0) {
      this.result.set({
        type: 'error',
        message: 'Không thể kết nối máy chủ. Vui lòng thử lại sau.'
      });
      return;
    }

    this.result.set({
      type: 'error',
      message: 'Có lỗi xảy ra. Vui lòng thử lại sau.'
    });
  }
}
