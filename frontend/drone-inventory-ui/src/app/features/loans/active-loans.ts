import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { LoanApi } from '../../core/loan-api';

/** The drones that are out right now (REQ-007). */
@Component({
  selector: 'app-active-loans',
  imports: [DatePipe, RouterLink],
  templateUrl: './active-loans.html',
  styleUrl: './active-loans.css',
})
export class ActiveLoans {
  private readonly loanApi = inject(LoanApi);

  protected readonly loans = rxResource({ stream: () => this.loanApi.list('Active') });
}
