import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DroneApi } from '../../core/drone-api';
import { LoanApi } from '../../core/loan-api';
import { Loan } from '../../core/loan.model';
import { problemMessage } from '../../core/problem-message';
import { StudentGroupApi } from '../../core/student-group-api';
import { DroneGlyph } from '../../shared/drone-glyph';
import { QuantityStepper } from '../../shared/quantity-stepper';

/** Form to loan a drone with batteries and propellers to a student group (REQ-004, REQ-006). */
@Component({
  selector: 'app-new-loan',
  imports: [FormField, FormRoot, RouterLink, NgTemplateOutlet, QuantityStepper, DroneGlyph],
  templateUrl: './new-loan.html',
  styleUrl: './new-loan.css',
})
export class NewLoan {
  private readonly droneApi = inject(DroneApi);
  private readonly studentGroupApi = inject(StudentGroupApi);
  private readonly loanApi = inject(LoanApi);

  protected readonly drones = rxResource({ stream: () => this.droneApi.list() });
  protected readonly groups = rxResource({ stream: () => this.studentGroupApi.list() });

  // The status comes from the backend. Loaned and damaged drones are listed in collapsed elements,
  // but stay selectable: the backend decides and answers 409 (REQ-005, REQ-004) – no rule is enforced here.
  protected readonly availableDrones = computed(() =>
    this.drones.hasValue() ? this.drones.value().filter((d) => d.status === 'Available') : [],
  );
  protected readonly loanedDrones = computed(() =>
    this.drones.hasValue() ? this.drones.value().filter((d) => d.status === 'Loaned') : [],
  );
  protected readonly damagedDrones = computed(() =>
    this.drones.hasValue() ? this.drones.value().filter((d) => d.status === 'Damaged') : [],
  );

  /** Radio values are strings; the ids are converted to numbers when the loan is sent. */
  protected readonly newLoan = signal({ droneId: '', studentGroupId: '', batteries: 0, propellers: 0 });

  /** Message from the backend, e.g. 409 drone already loaned or damaged, 422 negative quantity. */
  protected readonly saveError = signal('');

  /** The loan that was created last, for the confirmation text. */
  protected readonly created = signal<Loan | undefined>(undefined);

  /** The chosen drone and group, for the summary next to the button. */
  protected readonly chosenDrone = computed(() =>
    this.drones.hasValue() ? this.drones.value().find((d) => d.id.toString() === this.newLoan().droneId) : undefined,
  );
  protected readonly chosenGroup = computed(() =>
    this.groups.hasValue()
      ? this.groups.value().find((g) => g.id.toString() === this.newLoan().studentGroupId)
      : undefined,
  );

  /** Id from the button "Ausleihen" of a bay on the drones page (`/loans/new?drone=7`), used once. */
  private requestedDroneId = inject(ActivatedRoute).snapshot.queryParamMap.get('drone');

  // Preselects that drone as soon as the list is loaded – only if the backend lists it.
  private readonly preselect = effect(() => {
    const id = this.requestedDroneId;
    if (!id || !this.drones.hasValue()) {
      return;
    }
    this.requestedDroneId = null;
    if (this.drones.value().some((drone) => drone.id.toString() === id)) {
      untracked(() => this.newLoan.update((loan) => ({ ...loan, droneId: id })));
    }
  });

  /** Who gets which drone, or what is still missing – read back next to the button. */
  protected readonly summary = computed(() => {
    const drone = this.chosenDrone()?.name;
    const group = this.chosenGroup()?.name;
    if (drone && group) {
      return `${drone} für ${group}`;
    }
    if (drone) {
      return `${drone} – Gruppe wählen`;
    }
    return group ? `Drohne wählen – für ${group}` : 'Drohne und Gruppe wählen';
  });

  /** "4 Akkus, 8 Propeller" – the same wording in the summary and in the confirmation. */
  protected accessories(batteries: number, propellers: number): string {
    return `${batteries} ${batteries === 1 ? 'Akku' : 'Akkus'}, ${propellers} Propeller`;
  }

  protected readonly loanForm = form(
    this.newLoan,
    (loan) => {
      required(loan.droneId, { message: 'Drohne wählen.' });
      required(loan.studentGroupId, { message: 'Schülergruppe wählen.' });
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
            // The loaned drone now shows up under "Ausgeliehen".
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
