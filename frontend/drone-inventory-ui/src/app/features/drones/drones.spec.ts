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
      expect(create.request.body).toEqual({ name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', ssid: '' });
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
      expect(request.request.body).toEqual({ name: 'TELLO-07', description: 'Propellerschutz erneuert', ssid: '' });
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

  // Change support-damaged-drones, task 2.1: status Defekt (REQ-002), Defekt melden (REQ-014), Repariert (REQ-015).
  describe('damage', () => {
    const tello07: Drone = { id: 7, name: 'TELLO-07', description: '', status: 'Available', damageReason: null };
    const tello11: Drone = {
      id: 11,
      name: 'TELLO-11',
      description: 'SSID: TELLO-11',
      status: 'Damaged',
      damageReason: 'Propeller gebrochen',
    };
    const settle = () => new Promise((resolve) => setTimeout(resolve));
    const damageSheet = (page: HTMLElement) =>
      page.querySelector('dialog[aria-labelledby="damage-drone-heading"]') as HTMLDialogElement;
    const button = (page: HTMLElement, label: string) =>
      page.querySelector(`button[aria-label="${label}"]`) as HTMLButtonElement | null;

    async function submitReason(page: HTMLElement, reason: string) {
      const input = page.querySelector('#damage-reason') as HTMLTextAreaElement;
      input.value = reason;
      input.dispatchEvent(new Event('input'));
      page.querySelector('form.damage-form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    it('REQ-002: shows a damaged drone as Defekt with its reason, in a muted pill and not red', async () => {
      const page = await respond([tello07, tello11]);

      const bay = page.querySelector('.drone-list li:last-child')!;
      expect(bay.querySelector('.tag')?.textContent?.trim()).toBe('Defekt');
      expect(bay.querySelector('.tag')?.classList).toContain('tag-damaged');
      expect(bay.classList).toContain('is-damaged');
      expect(bay.querySelector('.damage')?.textContent?.trim()).toBe('Defekt: Propeller gebrochen');
      expect(bay.querySelector('.alert')).toBeNull();
      // No loan shortcut on a damaged bay; Repariert instead of Defekt melden.
      expect(bay.querySelector('a[aria-label="TELLO-11 ausleihen"]')).toBeNull();
      expect(button(page, 'TELLO-11 repariert')).not.toBeNull();
      expect(button(page, 'TELLO-11 defekt melden')).toBeNull();
      expect(button(page, 'TELLO-07 defekt melden')).not.toBeNull();
    });

    it('REQ-002: shows a loaned damaged drone as Ausgeliehen together with the reason', async () => {
      const page = await respond([{ ...tello07, status: 'Loaned', damageReason: 'Kamera ausgefallen' }]);

      const bay = page.querySelector('.drone-list li')!;
      expect(bay.querySelector('.tag')?.textContent?.trim()).toBe('Ausgeliehen');
      expect(bay.querySelector('.damage')?.textContent?.trim()).toBe('Defekt: Kamera ausgefallen');
      expect(button(page, 'TELLO-07 repariert')).not.toBeNull();
    });

    it('REQ-014: Defekt melden sends the reason and shows the drone as Defekt', async () => {
      const page = await respond([tello07]);
      button(page, 'TELLO-07 defekt melden')!.click();
      fixture.detectChanges();
      expect(damageSheet(page).open).toBe(true);
      expect(damageSheet(page).querySelector('h2')?.textContent).toBe('TELLO-07 defekt melden');

      await submitReason(page, 'Propeller gebrochen');
      const request = http.expectOne({ method: 'POST', url: '/api/drones/7/damage' });
      expect(request.request.body).toEqual({ reason: 'Propeller gebrochen' });
      const damaged: Drone = { ...tello07, status: 'Damaged', damageReason: 'Propeller gebrochen' };
      request.flush(damaged);
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([damaged]);
      await settle();
      fixture.detectChanges();

      expect(damageSheet(page).open).toBe(false);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-07 als defekt gemeldet.');
      expect(page.querySelector('.drone-list li .tag')?.textContent?.trim()).toBe('Defekt');
    });

    it('REQ-014: shows the backend message for an empty reason (422) in the sheet', async () => {
      const page = await respond([tello07]);
      button(page, 'TELLO-07 defekt melden')!.click();
      fixture.detectChanges();

      await submitReason(page, '   ');
      http.expectOne({ method: 'POST', url: '/api/drones/7/damage' }).flush(
        { status: 422, detail: 'Der Grund für den Defekt darf nicht leer sein.' },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
      await settle();
      fixture.detectChanges();

      expect(damageSheet(page).querySelector('[role="alert"]')?.textContent).toContain(
        'Der Grund für den Defekt darf nicht leer sein.',
      );
      expect(damageSheet(page).open).toBe(true);
      http.expectNone({ method: 'GET', url: '/api/drones' });
    });

    it('REQ-014: shows the backend message for an unknown drone (404) in the sheet', async () => {
      const page = await respond([tello07]);
      button(page, 'TELLO-07 defekt melden')!.click();
      fixture.detectChanges();

      await submitReason(page, 'Propeller gebrochen');
      http.expectOne({ method: 'POST', url: '/api/drones/7/damage' }).flush(
        { status: 404, detail: 'Die Drohne 7 gibt es nicht.' },
        { status: 404, statusText: 'Not Found' },
      );
      await settle();
      fixture.detectChanges();

      expect(damageSheet(page).querySelector('[role="alert"]')?.textContent).toContain('Die Drohne 7 gibt es nicht.');
    });

    it('does not send an empty reason and asks for one', async () => {
      const page = await respond([tello07]);
      button(page, 'TELLO-07 defekt melden')!.click();
      fixture.detectChanges();

      await submitReason(page, '');

      http.expectNone({ method: 'POST', url: '/api/drones/7/damage' });
      expect(damageSheet(page).querySelector('.field-error')?.textContent).toContain('Grund eingeben.');
    });

    it('REQ-015: Repariert sends DELETE and shows the drone as Verfügbar', async () => {
      const page = await respond([tello11]);

      button(page, 'TELLO-11 repariert')!.click();
      const request = http.expectOne({ method: 'DELETE', url: '/api/drones/11/damage' });
      const repaired: Drone = { ...tello11, status: 'Available', damageReason: null };
      request.flush(repaired);
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([repaired]);
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-11 ist repariert.');
      expect(page.querySelector('.drone-list li .tag')?.textContent?.trim()).toBe('Verfügbar');
      expect(page.querySelector('.drone-list li .damage')).toBeNull();
    });

    it('REQ-015: shows the backend message when the drone is not damaged (409)', async () => {
      const page = await respond([tello11]);

      button(page, 'TELLO-11 repariert')!.click();
      http.expectOne({ method: 'DELETE', url: '/api/drones/11/damage' }).flush(
        { status: 409, detail: 'Die Drohne "TELLO-11" ist nicht als defekt gemeldet.' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('[role="alert"]')?.textContent).toContain('ist nicht als defekt gemeldet.');
      http.expectNone({ method: 'GET', url: '/api/drones' });
    });

    it('REQ-015: shows the backend message for an unknown drone (404)', async () => {
      const page = await respond([tello11]);

      button(page, 'TELLO-11 repariert')!.click();
      http.expectOne({ method: 'DELETE', url: '/api/drones/11/damage' }).flush(
        { status: 404, detail: 'Die Drohne 11 gibt es nicht.' },
        { status: 404, statusText: 'Not Found' },
      );
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('[role="alert"]')?.textContent).toContain('Die Drohne 11 gibt es nicht.');
    });
  });

  // Change drone-ssid: "Drone SSID (REQ-024)" – SSID field in both sheets, SSID in the bay.
  describe('drone SSID', () => {
    const settle = () => new Promise((resolve) => setTimeout(resolve));
    const duplicate =
      'Eine Drohne mit der SSID "tello-9fa21b" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).';

    function type(page: HTMLElement, selector: string, value: string) {
      const input = page.querySelector(selector) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }

    async function submit(page: HTMLElement, formSelector: string) {
      page.querySelector(formSelector)!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    it('REQ-024: shows the SSID under the name and nothing for a drone without SSID', async () => {
      const page = await respond([
        { id: 7, name: 'TELLO-07', description: '', status: 'Available', ssid: 'TELLO-9FA21B' },
        { id: 8, name: 'TELLO-08', description: '', status: 'Available', ssid: null },
      ]);

      const ssids = [...page.querySelectorAll('.drone-list li')].map((row) => row.querySelector('.ssid')?.textContent?.trim());
      expect(ssids).toEqual(['SSID TELLO-9FA21B', undefined]);
    });

    it('REQ-024: sends the SSID of a new drone to the backend', async () => {
      const page = await respond([]);
      (page.querySelector('.page-head button') as HTMLButtonElement).click();

      type(page, '#drone-name', 'TELLO-07');
      type(page, '#drone-ssid', ' TELLO-9FA21B ');
      await submit(page, 'form');
      const create = http.expectOne({ method: 'POST', url: '/api/drones' });

      // Trimming is the backend's job (REQ-024); the field is sent as typed.
      expect(create.request.body).toEqual({ name: 'TELLO-07', description: '', ssid: ' TELLO-9FA21B ' });
      create.flush({ id: 7, name: 'TELLO-07', description: '', status: 'Available', ssid: 'TELLO-9FA21B' }, { status: 201, statusText: 'Created' });
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([]);
      await settle();
      fixture.detectChanges();
      expect((page.querySelector('#drone-ssid') as HTMLInputElement).value).toBe('');
    });

    it('REQ-024: shows the backend message for the SSID of another drone (409) when adding', async () => {
      const page = await respond([]);
      (page.querySelector('.page-head button') as HTMLButtonElement).click();

      type(page, '#drone-name', 'TELLO-08');
      type(page, '#drone-ssid', 'tello-9fa21b');
      await submit(page, 'form');
      http
        .expectOne({ method: 'POST', url: '/api/drones' })
        .flush({ status: 409, detail: duplicate }, { status: 409, statusText: 'Conflict' });
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('form [role="alert"]')?.textContent).toContain(duplicate);
      http.expectNone({ method: 'GET', url: '/api/drones' });
    });

    it('REQ-024: the edit sheet shows the SSID and sends the changed one', async () => {
      const page = await respond([{ id: 7, name: 'TELLO-07', description: '', status: 'Available', ssid: 'TELLO-9FA21B' }]);
      (page.querySelector('button[aria-label="TELLO-07 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect((page.querySelector('#edit-drone-ssid') as HTMLInputElement).value).toBe('TELLO-9FA21B');

      type(page, '#edit-drone-ssid', 'TELLO-1A2B3C');
      await submit(page, 'form.edit-form');
      const request = http.expectOne({ method: 'PUT', url: '/api/drones/7' });

      expect(request.request.body).toEqual({ name: 'TELLO-07', description: '', ssid: 'TELLO-1A2B3C' });
      request.flush({ id: 7, name: 'TELLO-07', description: '', status: 'Available', ssid: 'TELLO-1A2B3C' });
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/drones' }).flush([]);
    });

    it('REQ-024: a drone without SSID opens the edit sheet with an empty SSID field', async () => {
      const page = await respond([{ id: 8, name: 'TELLO-08', description: '', status: 'Available' }]);

      (page.querySelector('button[aria-label="TELLO-08 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      expect((page.querySelector('#edit-drone-ssid') as HTMLInputElement).value).toBe('');
    });

    it('REQ-024: shows the backend message for the SSID of another drone (409) in the edit sheet', async () => {
      const page = await respond([{ id: 8, name: 'TELLO-08', description: '', status: 'Available', ssid: null }]);
      (page.querySelector('button[aria-label="TELLO-08 bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      type(page, '#edit-drone-ssid', 'tello-9fa21b');
      await submit(page, 'form.edit-form');
      http
        .expectOne({ method: 'PUT', url: '/api/drones/8' })
        .flush({ status: 409, detail: duplicate }, { status: 409, statusText: 'Conflict' });
      await settle();
      fixture.detectChanges();

      const sheet = page.querySelector('dialog[aria-labelledby="edit-drone-heading"]') as HTMLDialogElement;
      expect(sheet.querySelector('[role="alert"]')?.textContent).toContain(duplicate);
      expect(sheet.open).toBe(true);
    });
  });
});
