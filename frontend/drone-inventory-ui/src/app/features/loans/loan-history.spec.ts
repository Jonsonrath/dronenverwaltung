import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Loan } from '../../core/loan.model';
import { LoanHistory } from './loan-history';

// Task 7.4: loan history page (REQ-010); returned and missing accessories:
// change return-differences task 2.2 (REQ-022).
describe('Loan history page', () => {
  let fixture: ComponentFixture<LoanHistory>;
  let http: HttpTestingController;

  // Most recent return first, as the backend sends it.
  const history: Loan[] = [
    {
      id: 2, droneId: 7, droneName: 'TELLO-07', studentGroupId: 3, studentGroupName: 'Group 3',
      batteries: 4, propellers: 8, loanDate: '2026-08-19T08:00:00Z', returnDate: '2026-08-26T14:30:00Z',
      status: 'Returned',
    },
    {
      id: 1, droneId: 2, droneName: 'TELLO-02', studentGroupId: 1, studentGroupName: 'Group 1',
      batteries: 3, propellers: 4, loanDate: '2026-08-19T08:00:00Z', returnDate: '2026-08-20T10:00:00Z',
      status: 'Returned',
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LoanHistory],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(LoanHistory);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function respond(body: Loan[] | null, status = 200) {
    fixture.detectChanges();
    const request = http.expectOne((r) => r.url === '/api/loans');
    expect(request.request.params.get('status')).toBe('Returned');
    if (status === 200) {
      request.flush(body);
    } else {
      request.flush(null, { status, statusText: 'Error' });
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('REQ-010: shows each returned loan with drone, group, accessories, loan date and return date', async () => {
    const page = await respond(history);

    const rows = [...page.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent?.trim()),
    );
    // Loans returned before REQ-022 have no returned quantities recorded and count as complete.
    expect(rows).toEqual([
      ['TELLO-07', 'Group 3', '4 von 4', '8 von 8', '19.08.2026', '26.08.2026'],
      ['TELLO-02', 'Group 1', '3 von 3', '4 von 4', '19.08.2026', '20.08.2026'],
    ]);
    expect(page.querySelector('.missing')).toBeNull();
  });

  it('REQ-010: shows a message when nothing has been returned yet', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('Noch wurde keine Ausleihe zurückgegeben.');
    expect(page.querySelector('table')).toBeNull();
  });

  it('shows an error when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('konnte nicht geladen werden');
  });

  // Change return-differences: "Record returned accessories (REQ-022)".
  it('REQ-022: complete return – 4 of 4 batteries and 8 of 8 propellers, nothing missing', async () => {
    const page = await respond([{ ...history[0], returnedBatteries: 4, returnedPropellers: 8 }]);

    const cells = [...page.querySelectorAll('tbody td.accessory')].map((cell) => cell.textContent?.trim());
    expect(cells).toEqual(['4 von 4', '8 von 8']);
    expect(page.querySelector('.missing')).toBeNull();
    expect(page.querySelector('tbody tr')?.classList.contains('has-missing')).toBe(false);
  });

  it('REQ-022: missing battery – 3 of 4 batteries with "1 fehlt", no propeller missing', async () => {
    const page = await respond([{ ...history[0], returnedBatteries: 3, returnedPropellers: 8 }]);

    const [batteries, propellers] = [...page.querySelectorAll('tbody td.accessory')];
    expect(batteries.textContent?.replace(/\s+/g, ' ').trim()).toBe('3 von 4 1 fehlt');
    expect(batteries.querySelector('.missing')?.textContent).toBe('1 fehlt');
    expect(propellers.textContent?.trim()).toBe('8 von 8');
    expect(propellers.querySelector('.missing')).toBeNull();
    expect(page.querySelector('tbody tr')?.classList.contains('has-missing')).toBe(true);
  });
});
