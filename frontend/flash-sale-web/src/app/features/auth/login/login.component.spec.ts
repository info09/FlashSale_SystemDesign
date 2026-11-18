import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';

describe('LoginComponent', () => {
  let authServiceMock: { login: ReturnType<typeof import('vitest').vi.fn> };

  beforeEach(async () => {
    authServiceMock = { login: vi.fn() };
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
      ],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should show validation error when form is invalid', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.errorMessage()).toContain('Vui lòng nhập đầy đủ');
    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('should call login and navigate on success', async () => {
    authServiceMock.login.mockReturnValue(of({ accessToken: 'token' }));
    const fixture = TestBed.createComponent(LoginComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    fixture.componentInstance.form.setValue({ userName: 'user', password: 'pass' });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(authServiceMock.login).toHaveBeenCalledWith({ userName: 'user', password: 'pass' });
    expect(navSpy).toHaveBeenCalled();
  });

  it('should show error message on login failure', async () => {
    authServiceMock.login.mockReturnValue(throwError(() => ({ status: 401, error: { message: 'Sai thông tin' } })));
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.componentInstance.form.setValue({ userName: 'user', password: 'wrong' });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.errorMessage()).toContain('Sai thông tin');
  });
});
