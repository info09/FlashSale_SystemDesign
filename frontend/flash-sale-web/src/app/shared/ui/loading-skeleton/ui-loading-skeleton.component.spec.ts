import { TestBed } from '@angular/core/testing';
import { UiLoadingSkeletonComponent } from './ui-loading-skeleton.component';

describe('UiLoadingSkeletonComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiLoadingSkeletonComponent],
    }).compileComponents();
  });

  it('should render default line skeletons', async () => {
    const fixture = TestBed.createComponent(UiLoadingSkeletonComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.skel.line').length).toBe(3);
  });

  it('should render table skeleton rows', async () => {
    const fixture = TestBed.createComponent(UiLoadingSkeletonComponent);
    fixture.componentRef.setInput('variant', 'table');
    fixture.componentRef.setInput('rows', 4);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.skel.table-head')).toBeTruthy();
    expect(el.querySelectorAll('.skel.table-row').length).toBe(4);
  });

  it('should render card and circle variants', async () => {
    const fixture = TestBed.createComponent(UiLoadingSkeletonComponent);

    fixture.componentRef.setInput('variant', 'card');
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('.skel.card')).toBeTruthy();

    fixture.componentRef.setInput('variant', 'circle');
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('.skel.circle')).toBeTruthy();
  });
});
