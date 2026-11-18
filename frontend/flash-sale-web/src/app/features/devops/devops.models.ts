export interface HealthResponse {
  status: string;
  checks: Record<string, string | HealthCheckDetail>;
}

export interface HealthCheckDetail {
  status?: string;
  description?: string;
  [key: string]: unknown;
}

export interface MetricSample {
  name: string;
  labels: Record<string, string>;
  value: number;
}

export interface MetricSnapshot {
  key: string;
  label: string;
  unit: string;
  samples: MetricSample[];
  latestValue: number | null;
}

export interface OutboxPurgeResponse {
  status?: string;
  code?: string;
  message?: string;
  deletedCount?: number;
}
