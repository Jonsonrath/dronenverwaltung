import { Component } from '@angular/core';

// Page shell from task 5.1; its content is built in a later task.
@Component({
  selector: 'app-loan-history',
  template: `
    <h1>Loan history</h1>
    <p class="lead">Every returned loan, most recent first.</p>
  `,
})
export class LoanHistory {}
