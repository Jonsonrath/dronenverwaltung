import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Clock } from '../../core/clock';
import { LoanApi } from '../../core/loan-api';
import { Loan } from '../../core/loan.model';
import { problemMessage } from '../../core/problem-message';

const MS_PER_DAY = 24 * 60 * 60 * 1000;

/** The drones that are out right now (REQ-007) and the action to return them (REQ-008). */
@Component({
  selector: 'app-active-loans',
  imports: [DatePipe, RouterLink],
  templateUrl: './active-loans.html',
  styleUrls: ['./loan-table.css', './active-loans.css'],
})
export class ActiveLoans {
  private readonly loanApi = inject(LoanApi);
  private readonly clock = inject(Clock);

  protected readonly loans = rxResource({ stream: () => this.loanApi.list('Active') });

  /** Id of the loan whose return is being sent, so its button cannot be pressed twice. */
  protected readonly returningId = signal<number | undefined>(undefined);

  /** Message from the backend when a return failed, e.g. 409 already returned. */
  protected readonly returnError = signal('');

  /** The loan that was returned last, for the confirmation text. */
  protected readonly returned = signal<Loan | undefined>(undefined);

  /** Full days since the loan date the backend stored. Only for display – there is no due date. */
  protected daysOut(loan: Loan): number {
    const elapsed = this.clock.now().getTime() - new Date(loan.loanDate).getTime();
    return Math.max(0, Math.floor(elapsed / MS_PER_DAY));
  }

  /** "seit heute", "seit 1 Tag", "seit 3 Tagen" */
  protected duration(loan: Loan): string {
    const days = this.daysOut(loan);
    if (days === 0) {
      return 'seit heute';
    }
    return days === 1 ? 'seit 1 Tag' : `seit ${days} Tagen`;
  }

  protected async returnLoan(loan: Loan) {
    this.returningId.set(loan.id);
    this.returnError.set('');
    this.returned.set(undefined);
    try {
      this.returned.set(await firstValueFrom(this.loanApi.returnLoan(loan.id)));
    } catch (error) {
      this.returnError.set(problemMessage(error));
    } finally {
      this.returningId.set(undefined);
      // Also after an error: the list may be outdated (e.g. returned somewhere else).
      this.loans.reload();
    }
  }
}
