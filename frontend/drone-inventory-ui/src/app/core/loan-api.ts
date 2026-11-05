import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateLoanRequest, Loan, LoanStatus } from './loan.model';

/** Backend calls for loans (design.md §6). */
@Service()
export class LoanApi {
  private readonly http = inject(HttpClient);

  /** Loans with the given status: Active (REQ-007) or Returned, the loan history (REQ-010). */
  list(status: LoanStatus): Observable<Loan[]> {
    return this.http.get<Loan[]>('/api/loans', { params: { status } });
  }

  /**
   * Loans a drone to a student group with batteries and propellers (REQ-004, REQ-006).
   * The backend rejects an already loaned drone (409, REQ-005), an unknown drone or
   * group (404) and a negative quantity (422).
   */
  create(request: CreateLoanRequest): Observable<Loan> {
    return this.http.post<Loan>('/api/loans', request);
  }

  /**
   * Returns a loan (REQ-008, REQ-009): the backend sets the ReturnDate and the drone is
   * available again. It rejects a loan that is already returned (409) or unknown (404).
   */
  returnLoan(id: number): Observable<Loan> {
    return this.http.post<Loan>(`/api/loans/${id}/return`, null);
  }
}
