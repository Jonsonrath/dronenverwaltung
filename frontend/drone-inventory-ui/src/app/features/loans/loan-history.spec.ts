import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Loan } from '../../core/loan.model';
import { LoanHistory } from './loan-history';

// Task 7.4: loan history page (REQ-010).
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

  it('REQ-010: shows each returned loan with group, drone, accessories, loan date and return date', async () => {
    const page = await respond(history);

    const rows = [...page.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026', '26.08.2026'],
      ['Group 1', 'TELLO-02', '3', '4', '19.08.2026', '20.08.2026'],
    ]);
  });

  it('REQ-010: shows a message when nothing has been returned yet', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('No loan has been returned yet.');
    expect(page.querySelector('table')).toBeNull();
  });

  it('shows an error when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');
  });
});
