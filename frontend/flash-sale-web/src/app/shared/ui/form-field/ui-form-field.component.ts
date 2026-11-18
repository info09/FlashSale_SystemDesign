import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'ui-form-field',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ui-form-field.component.html',
  styleUrl: './ui-form-field.component.scss'
})
export class UiFormFieldComponent {
  @Input() label = '';
  @Input() required = false;
  @Input() forId?: string;
  @Input() error?: string | null;
  @Input() hint?: string | null;
  @Input() loading = false;
}
