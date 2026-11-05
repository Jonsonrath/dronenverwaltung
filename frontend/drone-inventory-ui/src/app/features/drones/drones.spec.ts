import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Drone } from '../../core/drone.model';
import { Drones } from './drones';

// Drone page: list (task 6.1, REQ-002) and the form to add a drone (task 6.2, REQ-001).
describe('Drones page', () => {
  let fixture: ComponentFixture<Drones>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Drones],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(Drones);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function respond(body: Drone[] | null, status = 200) {
    fixture.detectChanges();
    const request = http.expectOne('/api/drones');
    if (status === 200) {
      request.flush(body);
    } else {
      request.flush(null, { status, statusText: 'Error' });
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('REQ-002: lists drones with name, status and description', async () => {
    const page = await respond([
      { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt', status: 'Available' },
    ]);

    const rows = [...page.querySelectorAll('.drone-list li')].map((row) =>
      ['.name', '.tag', '.description'].map((part) => row.querySelector(part)?.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['TELLO-02', 'Loaned', ''],
      ['TELLO-07', 'Available', 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt'],
    ]);
    expect(page.querySelector('.drone-list li:first-child .tag')?.classList).toContain('tag-loaned');
    expect(page.querySelector('.drone-list li:last-child .tag')?.classList).toContain('tag-available');
    expect(page.querySelector('.drone-list li:first-child')?.classList).toContain('is-loaned');
    expect(page.querySelector('.drone-list li:last-child')?.classList).toContain('is-available');
  });

  it('REQ-002: shows a message when there are no drones', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('No drones yet.');
    expect(page.querySelector('.drone-list')).toBeNull();
  });

  it('shows an error with a retry button when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');

    (page.querySelector('[role="alert"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    http.expectOne('/api/drones').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.querySelector('.empty')?.textContent).toContain('No drones yet.');
  });

  describe('add a drone', () => {
    const tello07: Drone = { id: 7, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', status: 'Available' };

    /** Let the async submit action run up to its next pending request. */
    const settle = () => new Promise((resolve) => setTimeout(resolve));

    async function fillAndSubmit(page: HTMLElement, name: string, description = '') {
      const nameInput = page.querySelector('#drone-name') as HTMLInputElement;
      const descriptionInput = page.querySelector('#drone-description') as HTMLTextAreaElement;
      nameInput.value = name;
      nameInput.dispatchEvent(new Event('input'));
      descriptionInput.value = description;
      descriptionInput.dispatchEvent(new Event('input'));
      page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    async function answer(request: ReturnType<HttpTestingController['expectOne']>, body: object, status = 201) {
      request.flush(body, { status, statusText: status === 201 ? 'Created' : 'Error' });
      await settle();
      fixture.detectChanges();
    }

    it('REQ-001: a new drone is sent to the backend and appears as Available', async () => {
      const page = await respond([]);

      await fillAndSubmit(page, 'TELLO-07', 'SSID: TELLO-9FA21B');
      const create = http.expectOne({ method: 'POST', url: '/api/drones' });
      expect(create.request.body).toEqual({ name: 'TELLO-07', description: 'SSID: TELLO-9FA21B' });
      await answer(create, tello07);

      // The list is loaded again and shows the new drone.
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([tello07]);
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('.drone-list li .name')?.textContent).toBe('TELLO-07');
      expect(page.querySelector('.drone-list li .tag')?.textContent).toBe('Available');
      expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-07 added.');
      expect((page.querySelector('#drone-name') as HTMLInputElement).value).toBe('');
    });

    it('REQ-001: shows the backend message when the name is rejected (422)', async () => {
      const page = await respond([]);

      await fillAndSubmit(page, '   ');
      await answer(http.expectOne({ method: 'POST', url: '/api/drones' }), {
        status: 422,
        title: 'Invalid input',
        detail: 'The name of a drone must not be empty.',
      }, 422);

      expect(page.querySelector('form [role="alert"]')?.textContent).toContain('The name of a drone must not be empty.');
      // Nothing was added, so the list is not loaded again.
      http.expectNone({ method: 'GET', url: '/api/drones' });
    });

    it('REQ-001: shows the backend message for a duplicate name (409)', async () => {
      const page = await respond([tello07]);

      await fillAndSubmit(page, 'tello-07');
      await answer(http.expectOne({ method: 'POST', url: '/api/drones' }), {
        status: 409,
        title: 'Conflict',
        detail: 'A drone with the name "tello-07" already exists (names are compared ignoring case).',
      }, 409);

      expect(page.querySelector('form [role="alert"]')?.textContent).toContain('already exists');
      expect((page.querySelector('#drone-name') as HTMLInputElement).value).toBe('tello-07');
    });

    it('removes an old backend message when the next attempt is invalid', async () => {
      const page = await respond([]);
      await fillAndSubmit(page, '   ');
      await answer(http.expectOne({ method: 'POST', url: '/api/drones' }), {
        status: 422,
        detail: 'The name of a drone must not be empty.',
      }, 422);

      await fillAndSubmit(page, '');

      expect(page.querySelector('form [role="alert"]')).toBeNull();
      expect(page.querySelector('.field-error')?.textContent).toContain('Enter a name.');
    });

    it('does not send an empty name and asks for one', async () => {
      const page = await respond([]);

      await fillAndSubmit(page, '');

      http.expectNone({ method: 'POST', url: '/api/drones' });
      expect(page.querySelector('.field-error')?.textContent).toContain('Enter a name.');
    });
  });
});
