import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Loan } from '../../core/loan.model';
import { ActiveLoans } from './active-loans';

// Task 7.2: active loans page (REQ-007).
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
      ['Group 1', 'TELLO-02', '3', '4', '19.08.2026'],
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026'],
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
});
