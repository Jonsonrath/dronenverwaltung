import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { LoanApi } from '../../core/loan-api';
import { problemMessage } from '../../core/problem-message';
import { StudentGroupApi } from '../../core/student-group-api';
import { StudentGroup } from '../../core/student-group.model';

/**
 * Student group list with the forms to add (REQ-003), rename (REQ-011) and delete (REQ-012) a group.
 * Every rule is checked by the backend; its messages are shown in the sheet that sent the request.
 */
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

  /** Confirmation after the last action that worked, e.g. "Group 3 hinzugefügt." */
  protected readonly notice = signal('');

  // The forms live in native dialogs: bottom sheets on phones, dialogs on desktop (design §8).
  private readonly addSheet = viewChild.required<ElementRef<HTMLDialogElement>>('addSheet');
  private readonly renameSheet = viewChild.required<ElementRef<HTMLDialogElement>>('renameSheet');
  private readonly deleteSheet = viewChild.required<ElementRef<HTMLDialogElement>>('deleteSheet');

  /** A click on the dimmed area around a sheet closes it; the dialog element itself is that area. */
  protected closeOnBackdrop(event: MouseEvent) {
    if (event.target instanceof HTMLDialogElement) {
      event.target.close();
    }
  }

  // ---- add (REQ-003) ----

  protected readonly newGroup = signal({ name: '' });

  /** Message from the backend when adding failed, e.g. 409 duplicate name or 422 blank name. */
  protected readonly saveError = signal('');

  protected openAdd() {
    this.saveError.set('');
    this.addSheet().nativeElement.showModal();
  }

  protected closeAdd() {
    this.addSheet().nativeElement.close();
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
          this.notice.set('');
          try {
            const group = await firstValueFrom(this.studentGroupApi.create({ name: this.newGroup().name }));
            this.notice.set(`${group.name} hinzugefügt.`);
            this.closeAdd();
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
          this.notice.set('');
        },
      },
    },
  );

  // ---- rename (REQ-011) ----

  /** The group whose name is being changed. */
  protected readonly renaming = signal<StudentGroup | undefined>(undefined);

  protected readonly newName = signal({ name: '' });

  /** Message from the backend when renaming failed, e.g. 409 name of another group. */
  protected readonly renameError = signal('');

  protected openRename(group: StudentGroup) {
    this.renaming.set(group);
    this.renameError.set('');
    this.renameForm().reset({ name: group.name });
    this.renameSheet().nativeElement.showModal();
  }

  protected closeRename() {
    this.renameSheet().nativeElement.close();
  }

  // Only a convenience check; the backend validates the name (REQ-011).
  protected readonly renameForm = form(
    this.newName,
    (group) => {
      required(group.name, { message: 'Name eingeben.' });
    },
    {
      submission: {
        action: async () => {
          const group = this.renaming();
          if (!group) {
            return undefined;
          }
          this.renameError.set('');
          this.notice.set('');
          try {
            const renamed = await firstValueFrom(this.studentGroupApi.rename(group.id, { name: this.newName().name }));
            this.notice.set(`${group.name} heißt jetzt ${renamed.name}.`);
            this.closeRename();
            this.groups.reload();
          } catch (error) {
            this.renameError.set(problemMessage(error));
          }
          return undefined;
        },
        onInvalid: () => this.renameError.set(''),
      },
    },
  );

  // ---- delete (REQ-012) ----

  /** The group that is about to be deleted. */
  protected readonly deleting = signal<StudentGroup | undefined>(undefined);

  /** Message from the backend when deleting failed, e.g. 409 the group has loans. */
  protected readonly deleteError = signal('');

  /** True while the delete request is on its way, so it cannot be sent twice. */
  protected readonly deleteBusy = signal(false);

  protected openDelete(group: StudentGroup) {
    this.deleting.set(group);
    this.deleteError.set('');
    this.deleteSheet().nativeElement.showModal();
  }

  protected closeDelete() {
    this.deleteSheet().nativeElement.close();
  }

  /** Always sent: whether the group may be deleted is decided by the backend (409 if it has loans). */
  protected async confirmDelete() {
    const group = this.deleting();
    if (!group) {
      return;
    }
    this.deleteBusy.set(true);
    this.deleteError.set('');
    this.notice.set('');
    try {
      await firstValueFrom(this.studentGroupApi.remove(group.id));
      this.notice.set(`${group.name} gelöscht.`);
      this.closeDelete();
      this.groups.reload();
    } catch (error) {
      this.deleteError.set(problemMessage(error));
    } finally {
      this.deleteBusy.set(false);
    }
  }
}
