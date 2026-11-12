import { DatePipe } from '@angular/common';
import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Clock } from '../../core/clock';
import { LoanApi } from '../../core/loan-api';
import { Loan } from '../../core/loan.model';
import { problemMessage } from '../../core/problem-message';
import { QuantityStepper } from '../../shared/quantity-stepper';

const MS_PER_DAY = 24 * 60 * 60 * 1000;

/**
 * The drones that are out right now (REQ-007) and the action to return them (REQ-008).
 * "Zurückgeben" opens a sheet for the returned batteries and propellers (REQ-022).
 */
@Component({
  selector: 'app-active-loans',
  imports: [DatePipe, RouterLink, QuantityStepper],
  templateUrl: './active-loans.html',
  styleUrls: ['./loan-table.css', './active-loans.css'],
})
export class ActiveLoans {
  private readonly loanApi = inject(LoanApi);
  private readonly clock = inject(Clock);

  protected readonly loans = rxResource({ stream: () => this.loanApi.list('Active') });

  /** The loan whose return is being entered in the sheet. */
  protected readonly returning = signal<Loan | undefined>(undefined);

  /** Returned accessories, prefilled with the loaned quantities (REQ-022). */
  protected readonly returnedBatteries = signal(0);
  protected readonly returnedPropellers = signal(0);

  /** True while the return is being sent, so it cannot be sent twice. */
  protected readonly sending = signal(false);

  /** Message from the backend when a return failed, e.g. 409 already returned or 422. */
  protected readonly returnError = signal('');

  /** The loan that was returned last, for the confirmation text. */
  protected readonly returned = signal<Loan | undefined>(undefined);

  private readonly returnSheet = viewChild.required<ElementRef<HTMLDialogElement>>('returnSheet');

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

  protected openReturn(loan: Loan) {
    this.returning.set(loan);
    this.returnedBatteries.set(loan.batteries);
    this.returnedPropellers.set(loan.propellers);
    this.returnError.set('');
    this.returnSheet().nativeElement.showModal();
  }

  protected closeReturn() {
    this.returnSheet().nativeElement.close();
  }

  /** A click on the dimmed area around the sheet closes it; the dialog element itself is that area. */
  protected closeOnBackdrop(event: MouseEvent) {
    if (event.target instanceof HTMLDialogElement) {
      event.target.close();
    }
  }

  /** "Fehlt: 1 Akku, 2 Propeller." from the numbers the backend stored; empty when nothing is missing. */
  protected missingText(loan: Loan): string {
    const batteries = loan.batteries - (loan.returnedBatteries ?? loan.batteries);
    const propellers = loan.propellers - (loan.returnedPropellers ?? loan.propellers);
    const parts: string[] = [];
    if (batteries > 0) {
      parts.push(batteries === 1 ? '1 Akku' : `${batteries} Akkus`);
    }
    if (propellers > 0) {
      parts.push(`${propellers} Propeller`);
    }
    return parts.length === 0 ? '' : `Fehlt: ${parts.join(', ')}.`;
  }

  protected async confirmReturn(event: Event) {
    event.preventDefault();
    const loan = this.returning();
    if (!loan || this.sending()) {
      return;
    }
    this.sending.set(true);
    this.returnError.set('');
    this.returned.set(undefined);
    try {
      const result = await firstValueFrom(
        this.loanApi.returnLoan(loan.id, {
          batteries: this.returnedBatteries(),
          propellers: this.returnedPropellers(),
        }),
      );
      this.returned.set(result);
      this.closeReturn();
    } catch (error) {
      // The sheet stays open with the backend's message, e.g. 422 more than loaned.
      this.returnError.set(problemMessage(error));
    } finally {
      this.sending.set(false);
      // Also after an error: the list may be outdated (e.g. returned somewhere else).
      this.loans.reload();
    }
  }
}
