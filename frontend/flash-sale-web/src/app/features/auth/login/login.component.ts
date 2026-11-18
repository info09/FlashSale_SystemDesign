import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Vui lòng nhập đầy đủ thông tin đăng nhập.');
      return;
    }

    this.loading.set(true);
    this.authService
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/buy';
          void this.router.navigateByUrl(returnUrl);
        },
        error: (err) => {
          this.errorMessage.set(this.resolveErrorMessage(err));
        }
      });
  }

  private resolveErrorMessage(err: any): string {
    const status = err?.status;
    if (status === 401 || status === 400) {
      return err?.error?.message || 'Tên đăng nhập hoặc mật khẩu không đúng.';
    }
    if (status === 0) {
      return 'Không thể kết nối máy chủ. Vui lòng thử lại sau.';
    }
    return err?.error?.message || err?.message || 'Có lỗi xảy ra. Vui lòng thử lại sau.';
  }
}
