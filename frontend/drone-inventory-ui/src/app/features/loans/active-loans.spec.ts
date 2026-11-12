import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Clock } from '../../core/clock';
import { Loan } from '../../core/loan.model';
import { ActiveLoans } from './active-loans';

// jsdom has no dialog methods; the page only needs "open" and "closed".
HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
  this.open = true;
};
HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
  this.open = false;
};

// Active loans page: list (task 7.2, REQ-007) and return action (task 7.3, REQ-008, REQ-009;
// return sheet with the returned accessories: change return-differences task 2.1, REQ-022).
describe('Active loans page', () => {
  let fixture: ComponentFixture<ActiveLoans>;
  let http: HttpTestingController;

  // The example from the assignment.
  const loans: Loan[] = [
    {
      id: 1, droneId: 2, droneName: 'TELLO-02', studentGroupId: 1, studentGroupName: 'Group 1',
      batteries: 3, propellers: 4, loanDate: '2026-08-19T08:00:00Z', returnDate: null, status: 'Active',
    },
    {
      id: 2, droneId: 7, droneName: 'TELLO-07', studentGroupId: 3, studentGroupName: 'Group 3',
      batteries: 4, propellers: 8, loanDate: '2026-08-19T08:15:00Z', returnDate: null, status: 'Active',
      responsibleStudentName: 'Anna Berger',
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ActiveLoans],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        // "Now" for the durations: 7 days after both loans of the example.
        { provide: Clock, useValue: { now: () => new Date('2026-08-26T12:00:00Z') } },
      ],
    });
    fixture = TestBed.createComponent(ActiveLoans);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function respond(body: Loan[] | null, status = 200) {
    fixture.detectChanges();
    const request = http.expectOne((r) => r.url === '/api/loans');
    expect(request.request.params.get('status')).toBe('Active');
    if (status === 200) {
      request.flush(body);
    } else {
      request.flush(null, { status, statusText: 'Error' });
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  // REQ-021: the responsible student at loan time; "–" when the group had none.
  it('REQ-007/REQ-021: shows drone, group, responsible student, batteries, propellers, loan date and duration of each active loan', async () => {
    const page = await respond(loans);

    const rows = [...page.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['TELLO-02', 'Group 1', '–', '3', '4', '19.08.2026', 'seit 7 Tagen', 'Zurückgeben'],
      ['TELLO-07', 'Group 3', 'Anna Berger', '4', '8', '19.08.2026', 'seit 7 Tagen', 'Zurückgeben'],
    ]);
  });

  it('marks a loan that has been running for more than a day', async () => {
    const page = await respond([
      { ...loans[0], loanDate: '2026-08-26T08:00:00Z' },
      { ...loans[1], loanDate: '2026-08-25T08:00:00Z' },
    ]);

    const durations = [...page.querySelectorAll('.duration')].map((d) => [
      d.textContent?.trim(),
      d.classList.contains('is-long'),
    ]);
    expect(durations).toEqual([
      ['seit heute', false],
      ['seit 1 Tag', true],
    ]);
  });

  it('REQ-007: shows a message when no drone is loaned', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('Gerade ist keine Drohne ausgeliehen.');
    expect(page.querySelector('table')).toBeNull();
  });

  it('shows an error when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('konnten nicht geladen werden');
  });

  // Return (REQ-008, REQ-009) through the return sheet with the returned accessories (REQ-022).
  describe('return a loan', () => {
    /** Let the async click handler run up to its next pending request. */
    const settle = () => new Promise((resolve) => setTimeout(resolve));

    const sheet = (page: HTMLElement) =>
      page.querySelector('dialog[aria-labelledby="return-heading"]') as HTMLDialogElement;
    const field = (page: HTMLElement, id: string) => page.querySelector(`#${id}`) as HTMLInputElement;

    async function openReturn(page: HTMLElement, droneName: string) {
      (page.querySelector(`button[aria-label="${droneName} zurückgeben"]`) as HTMLButtonElement).click();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    }

    function type(page: HTMLElement, id: string, value: string) {
      field(page, id).value = value;
      field(page, id).dispatchEvent(new Event('input'));
      fixture.detectChanges();
    }

    async function confirm(page: HTMLElement) {
      sheet(page).querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    async function reloadWith(body: Loan[]) {
      await settle();
      fixture.detectChanges();
      const reload = http.expectOne((r) => r.url === '/api/loans');
      expect(reload.request.params.get('status')).toBe('Active');
      reload.flush(body);
      await settle();
      fixture.detectChanges();
    }

    it('REQ-022: Zurückgeben opens the sheet prefilled with the loaned quantities and sends nothing yet', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');

      expect(sheet(page).open).toBe(true);
      expect(sheet(page).querySelector('h2')?.textContent).toBe('TELLO-07 zurückgeben');
      expect(field(page, 'return-batteries').value).toBe('4');
      expect(field(page, 'return-propellers').value).toBe('8');
      // + stops at the loaned quantity (only a convenience, the backend decides).
      expect((sheet(page).querySelector('button[aria-label="Mehr Akkus"]') as HTMLButtonElement).disabled).toBe(true);
      http.expectNone('/api/loans/2/return');
    });

    it('REQ-008/REQ-009/REQ-022: complete return sends the loaned quantities, closes the sheet and the list reloads', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      await confirm(page);
      const request = http.expectOne('/api/loans/2/return');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({ batteries: 4, propellers: 8 });
      request.flush({
        ...loans[1], status: 'Returned', returnDate: '2026-08-26T14:30:00Z', returnedBatteries: 4, returnedPropellers: 8,
      });
      await reloadWith([loans[0]]);

      const drones = [...page.querySelectorAll('tbody .drone')].map((cell) => cell.textContent);
      expect(drones).toEqual(['TELLO-02']);
      expect(sheet(page).open).toBe(false);
      const status = page.querySelector('[role="status"]')?.textContent;
      expect(status).toContain('TELLO-07 von Group 3 zurückgegeben. Sie ist wieder verfügbar.');
      expect(status).not.toContain('Fehlt');
    });

    it('REQ-022: missing battery – sends the changed number and the confirmation names what is missing', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      (sheet(page).querySelector('button[aria-label="Weniger Akkus"]') as HTMLButtonElement).click();
      fixture.detectChanges();
      await confirm(page);
      const request = http.expectOne('/api/loans/2/return');
      expect(request.request.body).toEqual({ batteries: 3, propellers: 8 });
      request.flush({
        ...loans[1], status: 'Returned', returnDate: '2026-08-26T14:30:00Z', returnedBatteries: 3, returnedPropellers: 8,
      });
      await reloadWith([loans[0]]);

      expect(page.querySelector('[role="status"] .missing')?.textContent).toBe('Fehlt: 1 Akku.');
    });

    it('REQ-022: shows the backend message (422) in the sheet and keeps it open', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      type(page, 'return-batteries', '5');
      await confirm(page);
      const request = http.expectOne('/api/loans/2/return');
      expect(request.request.body).toEqual({ batteries: 5, propellers: 8 });
      request.flush(
        {
          status: 422,
          title: 'Unprocessable Entity',
          detail: 'Die Anzahl der zurückgegebenen Akkus muss zwischen 0 und 4 liegen.',
        },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
      await reloadWith(loans);

      expect(sheet(page).open).toBe(true);
      expect(sheet(page).querySelector('[role="alert"]')?.textContent).toContain(
        'Die Anzahl der zurückgegebenen Akkus muss zwischen 0 und 4 liegen.',
      );
      expect(page.querySelector('[role="status"]')).toBeNull();
      expect(page.querySelectorAll('tbody tr').length).toBe(2);
    });

    it('REQ-008: shows the backend message when the loan was already returned (409)', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      await confirm(page);
      http.expectOne('/api/loans/2/return').flush(
        { status: 409, title: 'Conflict', detail: 'Die Ausleihe 2 wurde bereits zurückgegeben.' },
        { status: 409, statusText: 'Conflict' },
      );
      // The list is loaded again anyway, because it was outdated.
      await reloadWith([loans[0]]);

      expect(sheet(page).querySelector('[role="alert"]')?.textContent).toContain(
        'Die Ausleihe 2 wurde bereits zurückgegeben.',
      );
      expect(page.querySelector('[role="status"]')).toBeNull();
      expect(page.querySelectorAll('tbody tr').length).toBe(1);
    });

    it('disables the confirm button while the return is being sent', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      await confirm(page);

      const button = sheet(page).querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent?.trim()).toBe('Wird zurückgegeben…');
      http.expectOne('/api/loans/2/return').flush(loans[1]);
      await reloadWith([loans[0]]);
    });

    // Change damage-report-on-return: "Report damage on return (REQ-023)".
    it('REQ-023: return with damage report – sends the report and confirms that the drone is marked as damaged', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      expect(field(page, 'return-damage').value).toBe('');
      type(page, 'return-damage', 'Propeller gebrochen');
      await confirm(page);
      const request = http.expectOne('/api/loans/2/return');
      expect(request.request.body).toEqual({ batteries: 4, propellers: 8, damageReport: 'Propeller gebrochen' });
      request.flush({
        ...loans[1], status: 'Returned', returnDate: '2026-08-26T14:30:00Z', returnedBatteries: 4, returnedPropellers: 8,
        damageReport: 'Propeller gebrochen',
      });
      await reloadWith([loans[0]]);

      const status = page.querySelector('[role="status"]')?.textContent;
      expect(status).toContain('TELLO-07 zurückgegeben und als defekt markiert.');
      expect(status).not.toContain('wieder verfügbar');
    });

    it('REQ-023: whitespace-only report is sent as typed – the backend decides, the drone is available', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      type(page, 'return-damage', '   ');
      await confirm(page);
      const request = http.expectOne('/api/loans/2/return');
      expect(request.request.body).toEqual({ batteries: 4, propellers: 8, damageReport: '   ' });
      request.flush({
        ...loans[1], status: 'Returned', returnDate: '2026-08-26T14:30:00Z', returnedBatteries: 4, returnedPropellers: 8,
        damageReport: null,
      });
      await reloadWith([loans[0]]);

      expect(page.querySelector('[role="status"]')?.textContent).toContain(
        'TELLO-07 von Group 3 zurückgegeben. Sie ist wieder verfügbar.',
      );
    });

    it('REQ-023: the damage field is empty again when the next return is opened', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      type(page, 'return-damage', 'Propeller gebrochen');
      (sheet(page).querySelector('.sheet-actions .button:not(.button-primary)') as HTMLButtonElement).click();
      fixture.detectChanges();
      await openReturn(page, 'TELLO-02');

      expect(field(page, 'return-damage').value).toBe('');
      http.expectNone('/api/loans/2/return');
    });

    it('Abbrechen closes the sheet without a request', async () => {
      const page = await respond(loans);

      await openReturn(page, 'TELLO-07');
      const cancel = [...sheet(page).querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Abbrechen')!;
      cancel.click();
      fixture.detectChanges();

      expect(sheet(page).open).toBe(false);
      http.expectNone('/api/loans/2/return');
    });
  });
});
