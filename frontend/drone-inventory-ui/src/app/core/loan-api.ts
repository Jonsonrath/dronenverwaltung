import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateLoanRequest, Loan } from './loan.model';

/** Backend calls for loans (design.md §6). */
@Service()
export class LoanApi {
  private readonly http = inject(HttpClient);

  /**
   * Loans a drone to a student group with batteries and propellers (REQ-004, REQ-006).
   * The backend rejects an already loaned drone (409, REQ-005), an unknown drone or
   * group (404) and a negative quantity (422).
   */
  create(request: CreateLoanRequest): Observable<Loan> {
    return this.http.post<Loan>('/api/loans', request);
  }
}
