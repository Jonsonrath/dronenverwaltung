import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { LoanApi } from './loan-api';
import { Loan } from './loan.model';

// LoanApi calls the backend: create (task 7.1), list (task 7.2), returnLoan (task 7.3).
describe('LoanApi', () => {
  let api: LoanApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(LoanApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('REQ-007: list("Active") sends GET /api/loans?status=Active', async () => {
    const result = firstValueFrom(api.list('Active'));
    const request = http.expectOne('/api/loans?status=Active');
    request.flush([]);

    expect(request.request.method).toBe('GET');
    expect(await result).toEqual([]);
  });

  it('REQ-004/REQ-006: create() sends POST /api/loans with drone, group and accessories', async () => {
    const loan: Loan = {
      id: 1,
      droneId: 7,
      droneName: 'TELLO-07',
      studentGroupId: 3,
      studentGroupName: 'Group 3',
      batteries: 4,
      propellers: 8,
      loanDate: '2026-08-19T08:00:00Z',
      returnDate: null,
      status: 'Active',
    };

    const result = firstValueFrom(api.create({ droneId: 7, studentGroupId: 3, batteries: 4, propellers: 8 }));
    const request = http.expectOne('/api/loans');
    request.flush(loan);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ droneId: 7, studentGroupId: 3, batteries: 4, propellers: 8 });
    expect(await result).toEqual(loan);
  });

  it('REQ-008: returnLoan() sends POST /api/loans/{id}/return', async () => {
    const result = firstValueFrom(api.returnLoan(2));
    const request = http.expectOne('/api/loans/2/return');
    request.flush({ id: 2, status: 'Returned' });

    expect(request.request.method).toBe('POST');
    expect(await result).toEqual({ id: 2, status: 'Returned' });
  });
});
