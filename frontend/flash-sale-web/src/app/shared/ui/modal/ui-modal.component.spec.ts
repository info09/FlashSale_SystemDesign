import { TestBed } from '@angular/core/testing';
import { UiModalComponent } from './ui-modal.component';

describe('UiModalComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiModalComponent],
    }).compileComponents();
  });

  it('should not render when closed', async () => {
    const fixture = TestBed.createComponent(UiModalComponent);
    fixture.componentInstance.open = false;
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.ui-modal-backdrop')).toBeNull();
  });

  it('should render title and action buttons and emit events', async () => {
    const fixture = TestBed.createComponent(UiModalComponent);
    const component = fixture.componentInstance;
    component.open = true;
    component.title = 'Xác nhận';
    component.primaryLabel = 'Đồng ý';
    component.secondaryLabel = 'Hủy';
    let confirmed = 0;
    let cancelled = 0;
    let closed = 0;
    component.confirmed.subscribe(() => confirmed++);
    component.cancelled.subscribe(() => cancelled++);
    component.closed.subscribe(() => closed++);

    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.ui-modal-title')?.textContent).toContain('Xác nhận');
    const buttons = Array.from(el.querySelectorAll('.ui-modal-actions .btn')) as HTMLButtonElement[];
    const cancelBtn = buttons.find((b) => b.textContent?.includes('Hủy'));
    const confirmBtn = buttons.find((b) => b.textContent?.includes('Đồng ý'));
    const closeBtn = el.querySelector('.ui-modal-close') as HTMLButtonElement | null;

    cancelBtn?.click();
    confirmBtn?.click();
    closeBtn?.click();

    expect(cancelled).toBe(1);
    expect(confirmed).toBe(1);
    expect(closed).toBe(1);
  });

  it('should not emit when busy', async () => {
    const fixture = TestBed.createComponent(UiModalComponent);
    const component = fixture.componentInstance;
    component.open = true;
    component.primaryLabel = 'OK';
    component.secondaryLabel = 'Hủy';
    component.busy = true;
    let count = 0;
    component.confirmed.subscribe(() => count++);
    component.cancelled.subscribe(() => count++);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(el.querySelectorAll('.ui-modal-actions .btn')) as HTMLButtonElement[];
    buttons.forEach((b) => b.click());
    expect(count).toBe(0);
  });
});
