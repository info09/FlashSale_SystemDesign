import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { DevOpsDashboardComponent } from './devops-dashboard.component';
import { authInterceptor } from '../../../core/auth/auth.interceptor';

const metricsText = [
  '# HELP flash_sale_p99latency P99Latency metric',
  '# TYPE flash_sale_p99latency gauge',
  'flash_sale_p99latency{job="flashsale-api"} 12',
  '',
  '# HELP flash_sale_errorrate ErrorRate metric',
  '# TYPE flash_sale_errorrate gauge',
  'flash_sale_errorrate{job="flashsale-api"} 0',
  '',
  '# HELP flash_sale_queuedepth QueueDepth metric',
  '# TYPE flash_sale_queuedepth gauge',
  'flash_sale_queuedepth{job="flashsale-api"} 3',
  '',
  '# HELP flash_sale_dlqsize DlqSize metric',
  '# TYPE flash_sale_dlqsize gauge',
  'flash_sale_dlqsize{job="flashsale-api"} 1',
  '',
  '# HELP flash_sale_inventoryremaining InventoryRemaining metric',
  '# TYPE flash_sale_inventoryremaining gauge',
  'flash_sale_inventoryremaining{job="flashsale-api"} 25',
  ''
].join('\n');

describe('DevOps dashboard flow (UI + API integration)', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    localStorage.setItem('flashsale_access_token', 'test-token');

    await TestBed.configureTestingModule({
      imports: [DevOpsDashboardComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  function flushInitialLoad() {
    const healthReq = httpMock.expectOne('/api/v1/health');
    expect(healthReq.request.method).toBe('GET');
    healthReq.flush({ status: 'Healthy', checks: { Redis: 'Healthy', RabbitMQ: 'Healthy', PostgreSQLPrimary: 'Healthy' } });

    const metricsReq = httpMock.expectOne('/api/v1/metrics');
    expect(metricsReq.request.method).toBe('GET');
    metricsReq.flush(metricsText);
  }

  it('should load health and metrics from API and render dashboard data', async () => {
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    fixture.detectChanges();

    flushInitialLoad();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.health()?.status).toBe('Healthy');
    expect(component.metrics().length).toBe(5);

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('P99Latency');
    expect(el.textContent).toContain('DlqSize');
    expect(el.textContent).toContain('Có message lỗi cần xử lý trong DeadLetterQueue.');
  });

  it('should call purge API and show success result', async () => {
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    fixture.detectChanges();
    flushInitialLoad();
    fixture.detectChanges();
    await fixture.whenStable();

    fixture.componentInstance.requestPurge();
    fixture.componentInstance.confirmPurge();
    fixture.detectChanges();

    const purgeReq = httpMock.expectOne('/api/v1/outbox/purge');
    expect(purgeReq.request.method).toBe('POST');
    expect(purgeReq.request.headers.get('Authorization')).toBe('Bearer test-token');
    purgeReq.flush({ status: 'success', message: 'Đã xóa 2 bản ghi Outbox.', deletedCount: 2 });

    // refresh after purge triggers another health + metrics fetch
    const healthReq = httpMock.expectOne('/api/v1/health');
    healthReq.flush({ status: 'Healthy', checks: { Redis: 'Healthy' } });
    const metricsReq = httpMock.expectOne('/api/v1/metrics');
    metricsReq.flush(metricsText);

    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance.purgeResult()?.type).toBe('success');
    expect(fixture.componentInstance.purgeResult()?.message).toContain('Đã xóa 2 bản ghi Outbox.');
  });

  it('should handle unauthorized refresh by logging out and redirecting', async () => {
    const fixture = TestBed.createComponent(DevOpsDashboardComponent);
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();

    const healthReq = httpMock.expectOne('/api/v1/health');
    healthReq.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    const metricsReq = httpMock.expectOne('/api/v1/metrics');
    metricsReq.flush('', { status: 401, statusText: 'Unauthorized' });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(localStorage.getItem('flashsale_access_token')).toBeNull();
    expect(navSpy).toHaveBeenCalled();
  });
});
