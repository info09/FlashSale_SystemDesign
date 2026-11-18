import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { HealthResponse, MetricSample, MetricSnapshot, OutboxPurgeResponse } from './devops.models';

export const TRACKED_METRICS: Array<{ key: string; name: string; label: string; unit: string }> = [
  { key: 'p99latency', name: 'flash_sale_p99latency', label: 'P99Latency', unit: 'ms' },
  { key: 'errorrate', name: 'flash_sale_errorrate', label: 'ErrorRate', unit: '%' },
  { key: 'queuedepth', name: 'flash_sale_queuedepth', label: 'QueueDepth', unit: '' },
  { key: 'dlqsize', name: 'flash_sale_dlqsize', label: 'DlqSize', unit: '' },
  { key: 'inventoryremaining', name: 'flash_sale_inventoryremaining', label: 'InventoryRemaining', unit: '' }
];

@Injectable({ providedIn: 'root' })
export class DevOpsService {
  constructor(private readonly http: HttpClient) {}

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>('/api/v1/health');
  }

  getMetricsText(): Observable<string> {
    return this.http.get('/api/v1/metrics', { responseType: 'text' });
  }

  getMetricSnapshots(): Observable<MetricSnapshot[]> {
    return this.getMetricsText().pipe(map((text) => this.buildSnapshots(text)));
  }

  purgeOutbox(): Observable<OutboxPurgeResponse> {
    return this.http.post<OutboxPurgeResponse>('/api/v1/outbox/purge', {});
  }

  parsePrometheus(text: string): MetricSample[] {
    const samples: MetricSample[] = [];
    const lines = text.split(/\r?\n/);
    for (const raw of lines) {
      const line = raw.trim();
      if (!line || line.startsWith('#')) continue;

      // name{labels} value [timestamp] OR name value [timestamp]
      const match = line.match(/^([a-zA-Z_:][a-zA-Z0-9_:]*)(\{([^}]*)\})?\s+([-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?|[+-]?Inf|NaN)/);
      if (!match) continue;

      const name = match[1];
      const labelsRaw = match[3] || '';
      const value = Number(match[4]);
      const labels: Record<string, string> = {};
      if (labelsRaw) {
        const labelRegex = /([a-zA-Z_][a-zA-Z0-9_]*)="((?:\\.|[^"\\])*)"/g;
        let m: RegExpExecArray | null;
        while ((m = labelRegex.exec(labelsRaw)) !== null) {
          labels[m[1]] = m[2].replace(/\\"/g, '"').replace(/\\\\/g, '\\').replace(/\\n/g, '\n');
        }
      }
      samples.push({ name, labels, value: Number.isFinite(value) ? value : 0 });
    }
    return samples;
  }

  private buildSnapshots(text: string): MetricSnapshot[] {
    const samples = this.parsePrometheus(text);
    return TRACKED_METRICS.map((metric) => {
      const metricSamples = samples.filter((s) => s.name === metric.name);
      const latestValue = metricSamples.length
        ? metricSamples[metricSamples.length - 1].value
        : null;
      return {
        key: metric.key,
        label: metric.label,
        unit: metric.unit,
        samples: metricSamples,
        latestValue
      };
    });
  }
}
