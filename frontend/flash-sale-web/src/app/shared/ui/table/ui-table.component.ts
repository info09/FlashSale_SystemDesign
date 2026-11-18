import { Component, Input, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UiEmptyStateComponent } from '../empty-state/ui-empty-state.component';

export type UiStatusTone = 'success' | 'warning' | 'error' | 'info' | 'neutral';

export interface UiTableColumn {
  key: string;
  header: string;
  kind?: 'text' | 'status';
  statusTone?: (value: unknown, row: Record<string, unknown>) => UiStatusTone;
  format?: (value: unknown, row: Record<string, unknown>) => string;
}

@Component({
  selector: 'ui-table',
  standalone: true,
  imports: [CommonModule, UiEmptyStateComponent],
  templateUrl: './ui-table.component.html',
  styleUrl: './ui-table.component.scss'
})
export class UiTableComponent {
  @Input() columns: UiTableColumn[] = [];
  @Input() rows: Array<Record<string, unknown>> = [];
  @Input() loading = false;
  @Input() trackByKey = 'id';
  @Input() emptyTitle = 'Không có dữ liệu.';
  @Input() emptyDescription = '';
  @Input() emptyIcon = '📭';
  @Input() caption?: string;

  getCell(row: Record<string, unknown>, col: UiTableColumn): string {
    const value = row[col.key];
    if (col.format) return col.format(value, row);
    if (value === null || value === undefined || value === '') return '—';
    return String(value);
  }

  getStatusTone(row: Record<string, unknown>, col: UiTableColumn): UiStatusTone {
    const value = row[col.key];
    if (col.statusTone) return col.statusTone(value, row);
    const s = String(value ?? '').toLowerCase();
    if (['success', 'healthy', 'ok', 'published', 'accepted', '200', '202'].includes(s)) return 'success';
    if (['warning', 'pending', 'retry', '429', 'degraded'].includes(s)) return 'warning';
    if (['error', 'failed', 'unhealthy', 'dlq', '400', '401', '500', '503'].includes(s)) return 'error';
    if (['info', 'processing'].includes(s)) return 'info';
    return 'neutral';
  }

  trackRow(index: number, row: Record<string, unknown>): unknown {
    return row[this.trackByKey] ?? index;
  }
}
