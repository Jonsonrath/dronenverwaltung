import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { LoanApi } from '../../core/loan-api';
import { problemMessage } from '../../core/problem-message';
import { StudentApi } from '../../core/student-api';
import { StudentGroupApi } from '../../core/student-group-api';
import { StudentGroup } from '../../core/student-group.model';
import { Student } from '../../core/student.model';

/**
 * Student group list with the forms to add (REQ-003), rename (REQ-011) and delete (REQ-012) a group,
 * and the members of every group with the forms to add, move and remove a student (REQ-016 ... REQ-019).
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
  private readonly studentApi = inject(StudentApi);

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
  private readonly addStudentSheet = viewChild.required<ElementRef<HTMLDialogElement>>('addStudentSheet');
  private readonly studentSheet = viewChild.required<ElementRef<HTMLDialogElement>>('studentSheet');

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

  // ---- add student (REQ-016) ----

  /** The group that gets a new student. */
  protected readonly addingTo = signal<StudentGroup | undefined>(undefined);

  protected readonly newStudent = signal({ name: '', class: '' });

  /** Message from the backend when adding failed, e.g. 409 name already in the group or 422 blank name. */
  protected readonly studentError = signal('');

  protected openAddStudent(group: StudentGroup) {
    this.addingTo.set(group);
    this.studentError.set('');
    this.studentForm().reset({ name: '', class: '' });
    this.addStudentSheet().nativeElement.showModal();
  }

  protected closeAddStudent() {
    this.addStudentSheet().nativeElement.close();
  }

  // Only a convenience check; the backend validates the name (REQ-016).
  protected readonly studentForm = form(
    this.newStudent,
    (student) => {
      required(student.name, { message: 'Name eingeben.' });
    },
    {
      submission: {
        action: async () => {
          const group = this.addingTo();
          if (!group) {
            return undefined;
          }
          this.studentError.set('');
          this.notice.set('');
          try {
            const { name, class: schoolClass } = this.newStudent();
            const student = await firstValueFrom(this.studentApi.add(group.id, { name, class: schoolClass }));
            this.notice.set(`${student.name} ist jetzt in ${group.name}.`);
            this.closeAddStudent();
            this.groups.reload();
          } catch (error) {
            this.studentError.set(problemMessage(error));
          }
          return undefined;
        },
        onInvalid: () => this.studentError.set(''),
      },
    },
  );

  // ---- move (REQ-018) and remove (REQ-019) a student ----

  /** The student whose sheet is open, with the group it is in. */
  protected readonly managing = signal<{ student: Student; group: StudentGroup } | undefined>(undefined);

  /** The groups the open student can be moved to: all except its own. */
  protected readonly moveTargets = computed(() => {
    const managing = this.managing();
    if (!managing || !this.groups.hasValue()) {
      return [];
    }
    return this.groups.value().filter((group) => group.id !== managing.group.id);
  });

  /** Message from the backend when moving or removing failed, e.g. 409 name already in the target group. */
  protected readonly studentActionError = signal('');

  /** True while a move or remove request is on its way, so nothing is sent twice. */
  protected readonly studentBusy = signal(false);

  protected openStudent(student: Student, group: StudentGroup) {
    this.managing.set({ student, group });
    this.studentActionError.set('');
    this.studentSheet().nativeElement.showModal();
  }

  protected closeStudent() {
    this.studentSheet().nativeElement.close();
  }

  /** Always sent: whether the target group already has the name is decided by the backend (409). */
  protected async moveTo(target: StudentGroup) {
    const managing = this.managing();
    if (!managing) {
      return;
    }
    await this.studentAction(
      () => firstValueFrom(this.studentApi.move(managing.student.id, { studentGroupId: target.id })),
      `${managing.student.name} ist jetzt in ${target.name}.`,
    );
  }

  protected async removeStudent() {
    const managing = this.managing();
    if (!managing) {
      return;
    }
    await this.studentAction(
      () => firstValueFrom(this.studentApi.remove(managing.student.id)),
      `${managing.student.name} aus ${managing.group.name} entfernt.`,
    );
  }

  private async studentAction(request: () => Promise<unknown>, success: string) {
    this.studentBusy.set(true);
    this.studentActionError.set('');
    this.notice.set('');
    try {
      await request();
      this.notice.set(success);
      this.closeStudent();
      this.groups.reload();
    } catch (error) {
      this.studentActionError.set(problemMessage(error));
    } finally {
      this.studentBusy.set(false);
    }
  }
}
