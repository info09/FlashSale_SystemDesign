import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'ui-empty-state',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ui-empty-state.component.html',
  styleUrl: './ui-empty-state.component.scss'
})
export class UiEmptyStateComponent {
  @Input() icon = '📭';
  @Input() title = 'Không có dữ liệu.';
  @Input() description = '';
  @Input() actionLabel = '';
  @Output() action = new EventEmitter<void>();
}
