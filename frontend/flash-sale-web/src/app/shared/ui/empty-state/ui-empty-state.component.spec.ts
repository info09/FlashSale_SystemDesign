import { TestBed } from '@angular/core/testing';
import { UiEmptyStateComponent } from './ui-empty-state.component';

describe('UiEmptyStateComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiEmptyStateComponent],
    }).compileComponents();
  });

  it('should render default title and icon', async () => {
    const fixture = TestBed.createComponent(UiEmptyStateComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.ui-empty-icon')?.textContent).toContain('📭');
    expect(el.querySelector('.ui-empty-title')?.textContent).toContain('Không có dữ liệu.');
  });

  it('should render description and action button and emit action', async () => {
    const fixture = TestBed.createComponent(UiEmptyStateComponent);
    const component = fixture.componentInstance;
    component.title = 'Trống';
    component.description = 'Mô tả';
    component.actionLabel = 'Thử lại';
    let emitted = false;
    component.action.subscribe(() => (emitted = true));

    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.ui-empty-title')?.textContent).toContain('Trống');
    expect(el.querySelector('.ui-empty-desc')?.textContent).toContain('Mô tả');
    const button = el.querySelector('button.ui-empty-action') as HTMLButtonElement | null;
    expect(button?.textContent).toContain('Thử lại');
    button?.click();
    expect(emitted).toBe(true);
  });
});
