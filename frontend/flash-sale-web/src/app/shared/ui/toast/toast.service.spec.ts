import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    service = new ToastService();
  });

  it('should add and dismiss toasts', () => {
    const id = service.show('Hello', 'info', 0);
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0]).toMatchObject({ id, message: 'Hello', type: 'info' });

    service.dismiss(id);
    expect(service.toasts().length).toBe(0);
  });

  it('should create typed toasts', () => {
    service.success('ok', 0);
    service.error('err', 0);
    service.warning('warn', 0);
    expect(service.toasts().map((t) => t.type)).toEqual(['success', 'error', 'warning']);
  });
});
