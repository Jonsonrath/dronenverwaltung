import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { LoanApi } from '../../core/loan-api';
import { problemMessage } from '../../core/problem-message';
import { StudentGroupApi } from '../../core/student-group-api';

/** Student group list and the form to add a group (REQ-003). */
@Component({
  selector: 'app-student-groups',
  imports: [FormField, FormRoot],
  templateUrl: './student-groups.html',
  styleUrl: './student-groups.css',
})
export class StudentGroups {
  private readonly studentGroupApi = inject(StudentGroupApi);
  private readonly loanApi = inject(LoanApi);

  protected readonly groups = rxResource({ stream: () => this.studentGroupApi.list() });

  // Only to show which drone a group has right now. If this request fails, the badges are left out.
  private readonly activeLoans = rxResource({ stream: () => this.loanApi.list('Active') });

  /**
   * True once the active loans arrived. Before that – still loading or failed – no badge is shown:
   * "keine Drohne" would be a claim the page cannot back up.
   */
  protected readonly loansLoaded = computed(() => this.activeLoans.hasValue());

  /** Names of the drones each group has right now, by group id – read from the active loans. */
  protected readonly dronesByGroup = computed(() => {
    const drones = new Map<number, string[]>();
    if (this.activeLoans.hasValue()) {
      for (const loan of this.activeLoans.value()) {
        drones.set(loan.studentGroupId, [...(drones.get(loan.studentGroupId) ?? []), loan.droneName]);
      }
    }
    return drones;
  });

  protected readonly newGroup = signal({ name: '' });

  /** Message from the backend when adding failed, e.g. 409 duplicate name or 422 blank name. */
  protected readonly saveError = signal('');

  /** Name of the group that was added last, for the confirmation text. */
  protected readonly added = signal('');

  /** The add form lives in a native dialog: bottom sheet on phones, dialog on desktop (design §8). */
  private readonly sheet = viewChild.required<ElementRef<HTMLDialogElement>>('sheet');

  protected openSheet() {
    this.saveError.set('');
    this.sheet().nativeElement.showModal();
  }

  protected closeSheet() {
    this.sheet().nativeElement.close();
  }

  /** A click on the dimmed area around the sheet closes it; the dialog element itself is that area. */
  protected closeOnBackdrop(event: MouseEvent) {
    if (event.target === this.sheet().nativeElement) {
      this.closeSheet();
    }
  }

  // Only a convenience check; the backend validates the name (REQ-003).
  protected readonly groupForm = form(
    this.newGroup,
    (group) => {
      required(group.name, { message: 'Name eingeben.' });
    },
    {
      submission: {
        action: async () => {
          this.saveError.set('');
          this.added.set('');
          try {
            const group = await firstValueFrom(this.studentGroupApi.create({ name: this.newGroup().name }));
            this.added.set(group.name);
            this.closeSheet();
            this.groupForm().reset({ name: '' });
            this.groups.reload();
          } catch (error) {
            this.saveError.set(problemMessage(error));
          }
          return undefined;
        },
        // An old message from the backend must not stay next to the new hint.
        onInvalid: () => {
          this.saveError.set('');
          this.added.set('');
        },
      },
    },
  );
}
