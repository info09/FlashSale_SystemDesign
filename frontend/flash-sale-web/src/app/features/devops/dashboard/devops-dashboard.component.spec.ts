import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { DevOpsDashboardComponent } from './devops-dashboard.component';
import { DevOpsService } from '../devops.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../shared/ui/toast/toast.service';

describe('DevOpsDashboardComponent', () => {
  let devopsServiceMock: {
    getHealth: ReturnType<typeof vi.fn>;
    getMetricSnapshots: ReturnType<typeof vi.fn>;
    purgeOutbox: ReturnType<typeof vi.fn>;
  };
  let authServiceMock: { logout: ReturnType<typeof vi.fn> };
  let toastServiceMock: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const healthOk = { status: 'Healthy', checks: { Redis: 'Healthy', RabbitMQ: 'Healthy' } };
  const metricsOk = [
    { key: 'p99latency', label: 'P99Latency', unit: 'ms', latestValue: 12, samples: [{ name: 'flash_sale_p99latency', labels: { job: 'flashsale-api' }, value: 12 }] },
    { key: 'errorrate', label: 'ErrorRate', unit: '%', latestValue: 0, samples: [{ name: 'flash_sale_errorrate', labels: { job: 'flashsale-api' }, value: 0 }] },
    { key: 'queuedepth', label: 'QueueDepth', unit: '', latestValue: 1, samples: [{ name: 'flash_sale_queuedepth', labels: { job: 'flashsale-api' }, value: 1 }] },
    { key: 'dlqsize', label: 'DlqSize', unit: '', latestValue: 0, samples: [{ name: 'flash_sale_dlqsize', labels: { job: 'flashsale-api' }, value: 0 }] },
    { key: 'inventoryremaining', label: 'InventoryRemaining', unit: '', latestValue: 5, samples: [{ name: 'flash_sale_inventoryremaining', labels: { job: 'flashsale-api' }, value: 5 }] },
  ];

  beforeEach(async () => {
    devopsServiceMock = {
      getHealth: vi.fn().mockReturnValue(of(healthOk)),
      getMetricSnapshots: vi.fn().mockReturnValue(of(metricsOk)),
      purgeOutbox: vi.fn(),
    };
    authServiceMock = { logout: vi.fn() };
    toastServiceMock = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [DevOpsDashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DevOpsService, useValue: devopsServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: ToastService, useValue: toastServiceMock },
      ],
    }).compileComponents();
  });

  it('should create and load health/metrics on init', async () => {
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(devopsServiceMock.getHealth).toHaveBeenCalled();
    expect(devopsServiceMock.getMetricSnapshots).toHaveBeenCalled();
    expect(fixture.componentInstance.health()?.status).toBe('Healthy');
    expect(fixture.componentInstance.metrics().length).toBe(5);
  });

  it('should render metric table data', async () => {
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('P99Latency');
    expect(el.textContent).toContain('QueueDepth');
  });

  it('should show error message when metrics load fails', async () => {
    devopsServiceMock.getMetricSnapshots.mockReturnValue(throwError(() => ({ status: 500 })));
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.errorMessage()).toContain('Không thể tải Metric');
  });

  it('should confirm and purge outbox successfully', async () => {
    devopsServiceMock.purgeOutbox.mockReturnValue(of({ status: 'success', message: 'Đã xóa 2 ghi Outbox.', deletedCount: 2 }));
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    fixture.componentInstance.requestPurge();
    expect(fixture.componentInstance.purgeConfirming()).toBe(true);
    fixture.componentInstance.confirmPurge();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(devopsServiceMock.purgeOutbox).toHaveBeenCalled();
    expect(toastServiceMock.success).toHaveBeenCalled();
    expect(fixture.componentInstance.purgeResult()?.type).toBe('success');
  });

  it('should logout and redirect on 401 during refresh', async () => {
    devopsServiceMock.getHealth.mockReturnValue(throwError(() => ({ status: 401 })));
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalled();
  });
});
