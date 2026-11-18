import { Component, OnDestroy, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { finalize, forkJoin, of, catchError, map } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { DevOpsService } from '../devops.service';
import { HealthResponse, MetricSnapshot } from '../devops.models';
import { UiModalComponent } from '../../../shared/ui/modal/ui-modal.component';
import { UiEmptyStateComponent } from '../../../shared/ui/empty-state/ui-empty-state.component';
import { UiLoadingSkeletonComponent } from '../../../shared/ui/loading-skeleton/ui-loading-skeleton.component';
import { UiTableComponent, UiTableColumn } from '../../../shared/ui/table/ui-table.component';
import { ToastService } from '../../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-devops-dashboard',
  standalone: true,
  imports: [CommonModule, UiModalComponent, UiEmptyStateComponent, UiLoadingSkeletonComponent, UiTableComponent],
  templateUrl: './devops-dashboard.component.html',
  styleUrl: './devops-dashboard.component.scss'
})
export class DevOpsDashboardComponent implements OnInit, OnDestroy {
  private readonly devopsService = inject(DevOpsService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly toastService = inject(ToastService);

  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly health = signal<HealthResponse | null>(null);
  readonly metrics = signal<MetricSnapshot[]>([]);
  readonly lastUpdated = signal<Date | null>(null);

  readonly purgeLoading = signal(false);
  readonly purgeConfirming = signal(false);
  readonly purgeResult = signal<{ type: 'success' | 'error'; message: string } | null>(null);

  readonly grafanaUrl = 'http://localhost:3000/d/flashsale-overview/flashsale-overview?orgId=1&refresh=10s&from=now-15m&to=now';
  readonly grafanaEmbedUrl: SafeResourceUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.grafanaUrl);

  readonly metricColumns: UiTableColumn[] = [
    { key: 'label', header: 'Metric' },
    { key: 'value', header: 'Giá trị hiện tại' },
    { key: 'detail', header: 'Chi tiết theo job/sample' },
    {
      key: 'state',
      header: 'Trạng thái',
      kind: 'status',
      statusTone: (value) => {
        const s = String(value ?? '').toLowerCase();
        if (s === 'có dữ liệu') return 'success';
        if (s === 'không có dữ liệu') return 'warning';
        return 'neutral';
      }
    }
  ];

  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  readonly healthEntries = computed(() => {
    const checks = this.health()?.checks || {};
    return Object.entries(checks).map(([name, value]) => {
      const status = typeof value === 'string' ? value : (value?.status || 'Unknown');
      return { name, status, isHealthy: this.isHealthyStatus(status) };
    });
  });

  readonly metricRows = computed(() => {
    return this.metrics().map((m) => ({
      id: m.key,
      label: m.label,
      value: this.formatMetricSummary(m),
      detail: m.samples.length > 1 ? m.samples.map((s) => `${s.labels['job'] || s.labels['instance'] || 'sample'}=${this.formatValue(s.value, m.unit)}`).join(' | ') : (m.samples.length === 1 ? (m.samples[0].labels['job'] || 'sample') : '—'),
      state: m.samples.length ? 'Có dữ liệu' : 'Không có dữ liệu'
    }));
  });

  readonly overallHealthy = computed(() => {
    const h = this.health();
    if (!h) return false;
    if (typeof h.status === 'string') return this.isHealthyStatus(h.status);
    return this.healthEntries().every((e) => e.isHealthy);
  });

  readonly dlqSize = computed(() => this.getMetricValue('dlqsize'));
  readonly queueDepth = computed(() => this.getMetricValue('queuedepth'));
  readonly inventoryRemaining = computed(() => this.getMetricValue('inventoryremaining'));
  readonly p99Latency = computed(() => this.getMetricValue('p99latency'));
  readonly errorRate = computed(() => this.getMetricValue('errorrate'));

  ngOnInit(): void {
    this.refresh();
    this.refreshTimer = setInterval(() => this.refresh(true), 15000);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  refresh(silent = false): void {
    if (!silent) {
      this.loading.set(true);
      this.errorMessage.set(null);
    }

    forkJoin({
      health: this.devopsService.getHealth().pipe(
        map((data) => ({ data, error: null as HttpErrorResponse | null })),
        catchError((error: HttpErrorResponse) => of({ data: null as HealthResponse | null, error }))
      ),
      metrics: this.devopsService.getMetricSnapshots().pipe(
        map((data) => ({ data, error: null as HttpErrorResponse | null })),
        catchError((error: HttpErrorResponse) => of({ data: [] as MetricSnapshot[], error }))
      )
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ health, metrics }) => {
        const authError = [health.error, metrics.error].find((e) => e?.status === 401);
        if (authError) {
          this.handleUnauthorized();
          return;
        }

        if (health.data) this.health.set(health.data);
        if (metrics.data && metrics.data.length) this.metrics.set(metrics.data);
        if (health.data || (metrics.data && metrics.data.length)) {
          this.lastUpdated.set(new Date());
        }

        const errors: string[] = [];
        if (health.error) {
          if (health.error.status === 503 && health.error.error) {
            this.health.set(health.error.error as HealthResponse);
          } else if (!health.data) {
            errors.push('Không thể tải HealthCheck. Vui lòng kiểm tra kết nối ApiGateway, Redis, RabbitMQ và PostgreSQLPrimary.');
          }
        }
        if (metrics.error && (!metrics.data || !metrics.data.length)) {
          errors.push('Không thể tải Metric. Vui lòng kiểm tra endpoint /api/v1/metrics hoặc Prometheus.');
        }

        this.errorMessage.set(errors.length ? errors.join(' ') : null);
      });
  }

  requestPurge(): void {
    this.purgeResult.set(null);
    this.purgeConfirming.set(true);
  }

  cancelPurge(): void {
    this.purgeConfirming.set(false);
  }

  confirmPurge(): void {
    this.purgeConfirming.set(false);
    this.purgeLoading.set(true);
    this.purgeResult.set(null);
    this.devopsService
      .purgeOutbox()
      .pipe(finalize(() => this.purgeLoading.set(false)))
      .subscribe({
        next: (res) => {
          const count = res?.deletedCount ?? 0;
          const message = res?.message || `Đã xóa ${count} ghi Outbox đã xử lý và quá hạn.`;
          this.purgeResult.set({ type: 'success', message });
          this.toastService.success(message);
          this.refresh(true);
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 401) {
            this.handleUnauthorized();
            return;
          }
          const msg = err?.error?.message || 'Không thể purge Outbox. Vui lòng thử lại sau.';
          this.purgeResult.set({ type: 'error', message: msg });
          this.toastService.error(msg);
        }
      });
  }

  formatMetricSummary(metric: MetricSnapshot | undefined): string {
    if (!metric || metric.latestValue === null || metric.latestValue === undefined || !metric.samples.length) return '—';
    if (metric.samples.length === 1) return this.formatValue(metric.samples[0].value, metric.unit);
    return metric.samples.map((s) => `${s.labels['job'] || 'sample'}: ${this.formatValue(s.value, metric.unit)}`).join(' • ');
  }

  formatMetric(metric: MetricSnapshot | undefined): string {
    if (!metric || metric.latestValue === null || metric.latestValue === undefined) return '—';
    if (!metric.samples.length) return '—';
    if (metric.samples.length === 1) {
      return this.formatValue(metric.latestValue, metric.unit);
    }
    return metric.samples
      .map((s) => {
        const job = s.labels['job'] || s.labels['instance'] || 'sample';
        return `${job}: ${this.formatValue(s.value, metric.unit)}`;
      })
      .join(' • ');
  }

  formatValue(value: number | null, unit: string): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '—';
    const rounded = Math.abs(value) >= 100 ? Math.round(value) : Math.round(value * 100) / 100;
    return unit ? `${rounded} ${unit}` : `${rounded}`;
  }

  getMetricSnapshot(key: string): MetricSnapshot | undefined {
    return this.metrics().find((m) => m.key === key);
  }

  getMetricValue(key: string): number | null {
    const metric = this.getMetricSnapshot(key);
    if (!metric || !metric.samples.length) return metric?.latestValue ?? null;
    // for queue depth/dlq/inventory show max across jobs as quick signal
    return metric.samples.reduce((max, s) => Math.max(max, s.value), Number.NEGATIVE_INFINITY);
  }

  isHealthyStatus(status: string): boolean {
    const s = (status || '').toLowerCase();
    return s === 'healthy' || s === 'up' || s === 'ok';
  }

  trackByName(_: number, item: { name: string }): string {
    return item.name;
  }

  trackByKey(_: number, item: MetricSnapshot): string {
    return item.key;
  }

  private handleUnauthorized(): void {
    this.authService.logout();
    void this.router.navigate(['/login'], { queryParams: { reason: 'unauthorized', returnUrl: '/ops' } });
  }
}
