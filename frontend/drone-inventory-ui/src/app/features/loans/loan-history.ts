import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LoanApi } from '../../core/loan-api';
import { Loan } from '../../core/loan.model';

type Accessory = 'batteries' | 'propellers';

/**
 * Every returned loan, most recent return first; returned loans are never deleted (REQ-010).
 * Shows the returned accessories and what is missing (REQ-022) and the damage report (REQ-023).
 */
@Component({
  selector: 'app-loan-history',
  imports: [DatePipe],
  templateUrl: './loan-history.html',
  styleUrls: ['./loan-table.css', './loan-history.css'],
})
export class LoanHistory {
  private readonly loanApi = inject(LoanApi);

  protected readonly loans = rxResource({ stream: () => this.loanApi.list('Returned') });

  /**
   * Returned quantity as stored by the backend. Loans returned before REQ-022 have none
   * recorded (null) and count as complete.
   */
  protected returnedOf(loan: Loan, accessory: Accessory): number {
    const returned = accessory === 'batteries' ? loan.returnedBatteries : loan.returnedPropellers;
    return returned ?? loan[accessory];
  }

  /** Missing = loaned − returned; only displayed, the backend has already checked the numbers. */
  protected missingOf(loan: Loan, accessory: Accessory): number {
    return loan[accessory] - this.returnedOf(loan, accessory);
  }
}
