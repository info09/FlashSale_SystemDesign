import { TestBed } from '@angular/core/testing';
import { UiToastContainerComponent } from './ui-toast-container.component';
import { ToastService } from './toast.service';

describe('UiToastContainerComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiToastContainerComponent],
    }).compileComponents();
  });

  it('should render toasts and dismiss on close click', async () => {
    const fixture = TestBed.createComponent(UiToastContainerComponent);
    const service = TestBed.inject(ToastService);
    service.clear();
    service.error('Có lỗi xảy ra. Vui lòng thử lại sau.', 0);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Có lỗi xảy ra. Vui lòng thử lại sau.');
    const closeBtn = el.querySelector('.ui-toast-close') as HTMLButtonElement | null;
    expect(closeBtn).toBeTruthy();
    closeBtn?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(service.toasts().length).toBe(0);
  });
});
