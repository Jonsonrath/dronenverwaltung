import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DroneApi } from '../../core/drone-api';
import { LoanApi } from '../../core/loan-api';
import { Loan } from '../../core/loan.model';
import { problemMessage } from '../../core/problem-message';
import { StudentGroupApi } from '../../core/student-group-api';
import { QuantityStepper } from '../../shared/quantity-stepper';

/** Form to loan a drone with batteries and propellers to a student group (REQ-004, REQ-006). */
@Component({
  selector: 'app-new-loan',
  imports: [FormField, FormRoot, RouterLink, QuantityStepper],
  templateUrl: './new-loan.html',
  styleUrl: './new-loan.css',
})
export class NewLoan {
  private readonly droneApi = inject(DroneApi);
  private readonly studentGroupApi = inject(StudentGroupApi);
  private readonly loanApi = inject(LoanApi);

  protected readonly drones = rxResource({ stream: () => this.droneApi.list() });
  protected readonly groups = rxResource({ stream: () => this.studentGroupApi.list() });

  // The status comes from the backend. Loaned drones stay selectable: the backend
  // decides and answers 409 (REQ-005) – the frontend does not enforce the rule.
  protected readonly availableDrones = computed(() =>
    this.drones.hasValue() ? this.drones.value().filter((d) => d.status === 'Available') : [],
  );
  protected readonly loanedDrones = computed(() =>
    this.drones.hasValue() ? this.drones.value().filter((d) => d.status === 'Loaned') : [],
  );

  /** The two groups of the drone choice, in this order. */
  protected readonly droneGroups = computed(() => [
    { key: 'available', label: 'Available', drones: this.availableDrones() },
    { key: 'loaned', label: 'Loaned right now', drones: this.loanedDrones() },
  ]);

  /** Select values are strings; the ids are converted to numbers when the loan is sent. */
  protected readonly newLoan = signal({ droneId: '', studentGroupId: '', batteries: 0, propellers: 0 });

  /** Message from the backend, e.g. 409 drone already loaned or 422 negative quantity. */
  protected readonly saveError = signal('');

  /** The loan that was created last, for the confirmation text. */
  protected readonly created = signal<Loan | undefined>(undefined);

  protected readonly loanForm = form(
    this.newLoan,
    (loan) => {
      required(loan.droneId, { message: 'Choose a drone.' });
      required(loan.studentGroupId, { message: 'Choose a student group.' });
    },
    {
      submission: {
        action: async () => {
          this.saveError.set('');
          this.created.set(undefined);
          const { droneId, studentGroupId, batteries, propellers } = this.newLoan();
          try {
            const loan = await firstValueFrom(
              this.loanApi.create({
                droneId: Number(droneId),
                studentGroupId: Number(studentGroupId),
                batteries,
                propellers,
              }),
            );
            this.created.set(loan);
            this.loanForm().reset({ droneId: '', studentGroupId: '', batteries: 0, propellers: 0 });
            // The loaned drone now shows up under "Loaned right now".
            this.drones.reload();
          } catch (error) {
            this.saveError.set(problemMessage(error));
          }
          return undefined;
        },
        // An old message from the backend must not stay next to the new hints.
        onInvalid: () => {
          this.saveError.set('');
          this.created.set(undefined);
        },
      },
    },
  );
}
