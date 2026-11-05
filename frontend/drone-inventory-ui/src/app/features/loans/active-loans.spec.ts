import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Loan } from '../../core/loan.model';
import { ActiveLoans } from './active-loans';

// Active loans page: list (task 7.2, REQ-007) and return action (task 7.3, REQ-008, REQ-009).
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
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ActiveLoans],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
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

  it('REQ-007: shows group, drone, batteries, propellers and loan date of each active loan', async () => {
    const page = await respond(loans);

    const rows = [...page.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['Group 1', 'TELLO-02', '3', '4', '19.08.2026', 'Return'],
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026', 'Return'],
    ]);
  });

  it('REQ-007: shows a message when no drone is loaned', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('No drone is loaned right now.');
    expect(page.querySelector('table')).toBeNull();
  });

  it('shows an error when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');
  });

  describe('return a loan', () => {
    /** Let the async click handler run up to its next pending request. */
    const settle = () => new Promise((resolve) => setTimeout(resolve));

    async function clickReturn(page: HTMLElement, droneName: string) {
      (page.querySelector(`button[aria-label="Return ${droneName}"]`) as HTMLButtonElement).click();
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

    it('REQ-008/REQ-009: Return sends POST /api/loans/{id}/return and the list reloads', async () => {
      const page = await respond(loans);

      await clickReturn(page, 'TELLO-07');
      const request = http.expectOne('/api/loans/2/return');
      expect(request.request.method).toBe('POST');
      request.flush({ ...loans[1], status: 'Returned', returnDate: '2026-08-26T14:30:00Z' });
      await reloadWith([loans[0]]);

      const drones = [...page.querySelectorAll('tbody .drone')].map((cell) => cell.textContent);
      expect(drones).toEqual(['TELLO-02']);
      expect(page.querySelector('[role="status"]')?.textContent).toContain(
        'TELLO-07 returned by Group 3. It is available again.',
      );
    });

    it('REQ-008: shows the backend message when the loan was already returned (409)', async () => {
      const page = await respond(loans);

      await clickReturn(page, 'TELLO-07');
      http.expectOne('/api/loans/2/return').flush(
        { status: 409, title: 'Conflict', detail: 'Loan 2 has already been returned.' },
        { status: 409, statusText: 'Conflict' },
      );
      // The list is loaded again anyway, because it was outdated.
      await reloadWith([loans[0]]);

      expect(page.querySelector('[role="alert"]')?.textContent).toContain('Loan 2 has already been returned.');
      expect(page.querySelector('[role="status"]')).toBeNull();
      expect(page.querySelectorAll('tbody tr').length).toBe(1);
    });

    it('disables the button while the return is being sent', async () => {
      const page = await respond(loans);

      await clickReturn(page, 'TELLO-07');

      const button = page.querySelector('button[aria-label="Return TELLO-07"]') as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent?.trim()).toBe('Returning…');
      http.expectOne('/api/loans/2/return').flush(loans[1]);
      await reloadWith([loans[0]]);
    });
  });
});
