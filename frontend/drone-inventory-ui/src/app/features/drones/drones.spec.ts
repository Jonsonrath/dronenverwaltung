import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Drone } from '../../core/drone.model';
import { Drones } from './drones';

// jsdom has no dialog methods; the pages only need "open" and "closed".
HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
  this.open = true;
};
HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
  this.open = false;
};

// Drone page: list (task 6.1, REQ-002) and the form to add a drone (task 6.2, REQ-001).
describe('Drones page', () => {
  let fixture: ComponentFixture<Drones>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Drones],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
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
      ['TELLO-02', 'Ausgeliehen', ''],
      ['TELLO-07', 'Verfügbar', 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt'],
    ]);
    expect(page.querySelector('.drone-list li:first-child .tag')?.classList).toContain('tag-loaned');
    expect(page.querySelector('.drone-list li:last-child .tag')?.classList).toContain('tag-available');
    expect(page.querySelector('.drone-list li:first-child')?.classList).toContain('is-loaned');
    expect(page.querySelector('.drone-list li:last-child')?.classList).toContain('is-available');
  });

  it('counts how many of the listed drones are in the cabinet', async () => {
    const page = await respond([
      { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: '', status: 'Available' },
    ]);

    expect(page.querySelector('.cabinet-count')?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
      '1 von 2 im Schrank',
    );
  });

  it('links an available drone to the loan form and a loaned drone to the active loans', async () => {
    const page = await respond([
      { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: '', status: 'Available' },
    ]);

    const links = [...page.querySelectorAll('.drone-list li a')].map((a) => [
      a.getAttribute('aria-label'),
      a.getAttribute('href'),
    ]);
    expect(links).toEqual([
      ['TELLO-02 zurückgeben', '/loans'],
      ['TELLO-07 ausleihen', '/loans/new?drone=2'],
    ]);
  });

  it('REQ-002: shows a message when there are no drones', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('Noch keine Drohnen.');
    expect(page.querySelector('.drone-list')).toBeNull();
  });

  it('shows an error with a retry button when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('konnten nicht geladen werden');

    (page.querySelector('[role="alert"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    http.expectOne('/api/drones').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.querySelector('.empty')?.textContent).toContain('Noch keine Drohnen.');
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

    it('opens the form as a sheet from the button in the page head', async () => {
      const page = await respond([]);
      const sheet = page.querySelector('dialog') as HTMLDialogElement;
      expect(sheet.open).toBe(false);

      (page.querySelector('.page-head button') as HTMLButtonElement).click();

      expect(sheet.open).toBe(true);
      expect(sheet.querySelector('#drone-name')).not.toBeNull();
    });

    it('REQ-001: a new drone is sent to the backend and appears as Available', async () => {
      const page = await respond([]);
      (page.querySelector('.page-head button') as HTMLButtonElement).click();

      await fillAndSubmit(page, 'TELLO-07', 'SSID: TELLO-9FA21B');
      const create = http.expectOne({ method: 'POST', url: '/api/drones' });
      expect(create.request.body).toEqual({ name: 'TELLO-07', description: 'SSID: TELLO-9FA21B' });
      await answer(create, tello07);

      // The list is loaded again and shows the new drone.
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([tello07]);
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('.drone-list li .name')?.textContent).toBe('TELLO-07');
      expect(page.querySelector('.drone-list li .tag')?.textContent).toBe('Verfügbar');
      expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-07 hinzugefügt.');
      // The sheet closes after the drone was added.
      expect((page.querySelector('dialog') as HTMLDialogElement).open).toBe(false);
      expect((page.querySelector('#drone-name') as HTMLInputElement).value).toBe('');
    });

    it('REQ-001: shows the backend message when the name is rejected (422)', async () => {
      const page = await respond([]);

      await fillAndSubmit(page, '   ');
      await answer(http.expectOne({ method: 'POST', url: '/api/drones' }), {
        status: 422,
        title: 'Invalid input',
        detail: 'Der Name einer Drohne darf nicht leer sein.',
      }, 422);

      expect(page.querySelector('form [role="alert"]')?.textContent).toContain('Der Name einer Drohne darf nicht leer sein.');
      // Nothing was added, so the list is not loaded again.
      http.expectNone({ method: 'GET', url: '/api/drones' });
    });

    it('REQ-001: shows the backend message for a duplicate name (409)', async () => {
      const page = await respond([tello07]);

      await fillAndSubmit(page, 'tello-07');
      await answer(http.expectOne({ method: 'POST', url: '/api/drones' }), {
        status: 409,
        title: 'Conflict',
        detail: 'Eine Drohne mit dem Namen "tello-07" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).',
      }, 409);

      expect(page.querySelector('form [role="alert"]')?.textContent).toContain('gibt es bereits');
      expect((page.querySelector('#drone-name') as HTMLInputElement).value).toBe('tello-07');
    });

    it('removes an old backend message when the next attempt is invalid', async () => {
      const page = await respond([]);
      await fillAndSubmit(page, '   ');
      await answer(http.expectOne({ method: 'POST', url: '/api/drones' }), {
        status: 422,
        detail: 'Der Name einer Drohne darf nicht leer sein.',
      }, 422);

      await fillAndSubmit(page, '');

      expect(page.querySelector('form [role="alert"]')).toBeNull();
      expect(page.querySelector('.field-error')?.textContent).toContain('Name eingeben.');
    });

    it('does not send an empty name and asks for one', async () => {
      const page = await respond([]);

      await fillAndSubmit(page, '');

      http.expectNone({ method: 'POST', url: '/api/drones' });
      expect(page.querySelector('.field-error')?.textContent).toContain('Name eingeben.');
    });
  });

  // Change edit-drones: "Edit drone (REQ-013)" – Bearbeiten on every bay opens a sheet.
  describe('edit a drone', () => {
    const tello07: Drone = { id: 7, name: 'TELO-07', description: 'rechter Propellerschutz beschädigt', status: 'Available' };
    const settle = () => new Promise((resolve) => setTimeout(resolve));
    const editSheet = (page: HTMLElement) =>
      page.querySelector('dialog[aria-labelledby="edit-drone-heading"]') as HTMLDialogElement;

    async function submitEdit(page: HTMLElement, name: string, description: string) {
      const nameInput = page.querySelector('#edit-drone-name') as HTMLInputElement;
      const descriptionInput = page.querySelector('#edit-drone-description') as HTMLTextAreaElement;
      nameInput.value = name;
      nameInput.dispatchEvent(new Event('input'));
      descriptionInput.value = description;
      descriptionInput.dispatchEvent(new Event('input'));
      page.querySelector('form.edit-form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    it('REQ-013: opens the edit sheet with name and description of the drone', async () => {
      const page = await respond([tello07]);

      (page.querySelector('button[aria-label="TELO-07 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      expect(editSheet(page).open).toBe(true);
      expect(editSheet(page).querySelector('h2')?.textContent).toBe('TELO-07 bearbeiten');
      expect((page.querySelector('#edit-drone-name') as HTMLInputElement).value).toBe('TELO-07');
      expect((page.querySelector('#edit-drone-description') as HTMLTextAreaElement).value).toBe(
        'rechter Propellerschutz beschädigt',
      );
    });

    it('REQ-013: sends the changes to the backend and shows the renamed drone', async () => {
      const page = await respond([tello07]);
      (page.querySelector('button[aria-label="TELO-07 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      await submitEdit(page, 'TELLO-07', 'Propellerschutz erneuert');
      const request = http.expectOne({ method: 'PUT', url: '/api/drones/7' });
      expect(request.request.body).toEqual({ name: 'TELLO-07', description: 'Propellerschutz erneuert' });
      const edited = { ...tello07, name: 'TELLO-07', description: 'Propellerschutz erneuert' };
      request.flush(edited);
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([edited]);
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('.drone-list li .name')?.textContent).toBe('TELLO-07');
      expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-07 gespeichert.');
      expect(editSheet(page).open).toBe(false);
    });

    it('REQ-013: shows the backend message for the name of another drone (409) in the sheet', async () => {
      const page = await respond([tello07]);
      (page.querySelector('button[aria-label="TELO-07 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      await submitEdit(page, 'TELLO-02', '');
      http.expectOne({ method: 'PUT', url: '/api/drones/7' }).flush(
        { status: 409, detail: 'Eine Drohne mit dem Namen "TELLO-02" gibt es bereits.' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      expect(editSheet(page).querySelector('[role="alert"]')?.textContent).toContain('gibt es bereits');
      expect(editSheet(page).open).toBe(true);
      http.expectNone({ method: 'GET', url: '/api/drones' });
    });

    it('does not send an empty name and asks for one', async () => {
      const page = await respond([tello07]);
      (page.querySelector('button[aria-label="TELO-07 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      await submitEdit(page, '', '');

      http.expectNone({ method: 'PUT', url: '/api/drones/7' });
      expect(editSheet(page).querySelector('.field-error')?.textContent).toContain('Name eingeben.');
    });
  });
});
