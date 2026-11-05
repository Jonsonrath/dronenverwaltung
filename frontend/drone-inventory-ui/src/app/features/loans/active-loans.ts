import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LoanApi } from '../../core/loan-api';
import { Loan } from '../../core/loan.model';
import { problemMessage } from '../../core/problem-message';

/** The drones that are out right now (REQ-007) and the action to return them (REQ-008). */
@Component({
  selector: 'app-active-loans',
  imports: [DatePipe, RouterLink],
  templateUrl: './active-loans.html',
  styleUrl: './active-loans.css',
})
export class ActiveLoans {
  private readonly loanApi = inject(LoanApi);

  protected readonly loans = rxResource({ stream: () => this.loanApi.list('Active') });

  /** Id of the loan whose return is being sent, so its button cannot be pressed twice. */
  protected readonly returningId = signal<number | undefined>(undefined);

  /** Message from the backend when a return failed, e.g. 409 already returned. */
  protected readonly returnError = signal('');

  /** The loan that was returned last, for the confirmation text. */
  protected readonly returned = signal<Loan | undefined>(undefined);

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
