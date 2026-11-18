import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'ui-loading-skeleton',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ui-loading-skeleton.component.html',
  styleUrl: './ui-loading-skeleton.component.scss'
})
export class UiLoadingSkeletonComponent {
  @Input() variant: 'line' | 'card' | 'table' | 'circle' = 'line';
  @Input() lines = 3;
  @Input() rows = 4;
  @Input() height = '16px';
  @Input() width = '100%';

  get lineItems(): number[] {
    return Array.from({ length: Math.max(1, this.lines) }, (_, i) => i);
  }

  get rowItems(): number[] {
    return Array.from({ length: Math.max(1, this.rows) }, (_, i) => i);
  }
}
