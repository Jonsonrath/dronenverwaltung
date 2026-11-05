import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LoanApi } from '../../core/loan-api';

/** Every returned loan, most recent return first; returned loans are never deleted (REQ-010). */
@Component({
  selector: 'app-loan-history',
  imports: [DatePipe],
  templateUrl: './loan-history.html',
  styleUrls: ['./loan-table.css', './loan-history.css'],
})
export class LoanHistory {
  private readonly loanApi = inject(LoanApi);

  protected readonly loans = rxResource({ stream: () => this.loanApi.list('Returned') });
}
