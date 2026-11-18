import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UiFormFieldComponent } from './ui-form-field.component';

@Component({
  standalone: true,
  imports: [UiFormFieldComponent],
  template: `
    <ui-form-field [label]="label" [required]="required" [forId]="forId" [error]="error" [hint]="hint" [loading]="loading">
      <input [id]="forId" type="text" />
    </ui-form-field>
  `,
})
class HostComponent {
  label = 'Tên';
  required = true;
  forId = 'name';
  error: string | null = null;
  hint: string | null = null;
  loading = false;
}

describe('UiFormFieldComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
    }).compileComponents();
  });

  it('should render label, required marker and projected input', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const label = el.querySelector('label.ui-label');
    expect(label?.getAttribute('for')).toBe('name');
    expect(label?.textContent).toContain('Tên');
    expect(label?.textContent).toContain('*');
    expect(el.querySelector('input#name')).toBeTruthy();
  });

  it('should show error message when error is provided', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.error = 'Lỗi bắt buộc';
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.ui-error')?.textContent).toContain('Lỗi bắt buộc');
  });

  it('should show hint and loading spinner', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.hint = 'Gợi ý';
    fixture.componentInstance.loading = true;
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.ui-hint')?.textContent).toContain('Gợi ý');
    expect(el.querySelector('.ui-inline-spinner')).toBeTruthy();
  });
});
