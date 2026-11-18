import { TestBed } from '@angular/core/testing';
import { UiTableColumn, UiTableComponent } from './ui-table.component';

describe('UiTableComponent', () => {
  const columns: UiTableColumn[] = [
    { key: 'name', header: 'Tên' },
    { key: 'status', header: 'Trạng thái', kind: 'status' },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiTableComponent],
    }).compileComponents();
  });

  it('should render headers and rows', async () => {
    const fixture = TestBed.createComponent(UiTableComponent);
    fixture.componentInstance.columns = columns;
    fixture.componentInstance.rows = [{ id: '1', name: 'Metric A', status: 'Healthy' }];
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const headers = Array.from(el.querySelectorAll('th')).map((th) => th.textContent?.trim());
    expect(headers).toEqual(['Tên', 'Trạng thái']);
    expect(el.textContent).toContain('Metric A');
    expect(el.querySelector('.status-badge')?.textContent).toContain('Healthy');
  });

  it('should render empty state when no rows', async () => {
    const fixture = TestBed.createComponent(UiTableComponent);
    fixture.componentInstance.columns = columns;
    fixture.componentInstance.rows = [];
    fixture.componentInstance.emptyTitle = 'Không có dữ liệu.';
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Không có dữ liệu.');
  });

  it('should render loading skeleton rows when loading', async () => {
    const fixture = TestBed.createComponent(UiTableComponent);
    fixture.componentInstance.columns = columns;
    fixture.componentInstance.loading = true;
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('tr.skeleton-row').length).toBe(3);
  });
});
