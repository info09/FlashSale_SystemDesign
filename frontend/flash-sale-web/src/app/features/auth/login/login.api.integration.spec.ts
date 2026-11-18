import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { LoginComponent } from './login.component';

describe('Login flow (UI + API integration)', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
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

  it('should call login API, store token and navigate on success', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({ userName: 'admin', password: '123456' });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userName: 'admin', password: '123456' });

    req.flush({ accessToken: 'jwt-token-123' });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(localStorage.getItem('flashsale_access_token')).toBe('jwt-token-123');
    expect(navSpy).toHaveBeenCalled();
  });

  it('should show error message when login API fails', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({ userName: 'admin', password: 'wrong' });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush({ message: 'Sai thông tin đăng nhập.' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(localStorage.getItem('flashsale_access_token')).toBeNull();
    expect(fixture.componentInstance.errorMessage()).toContain('Sai thông tin đăng nhập.');
  });
});
