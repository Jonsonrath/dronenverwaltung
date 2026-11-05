import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
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

  protected readonly groups = rxResource({ stream: () => this.studentGroupApi.list() });

  protected readonly newGroup = signal({ name: '' });

  /** Message from the backend when adding failed, e.g. 409 duplicate name or 422 blank name. */
  protected readonly saveError = signal('');

  /** Name of the group that was added last, for the confirmation text. */
  protected readonly added = signal('');

  // Only a convenience check; the backend validates the name (REQ-003).
  protected readonly groupForm = form(
    this.newGroup,
    (group) => {
      required(group.name, { message: 'Enter a name.' });
    },
    {
      submission: {
        action: async () => {
          this.saveError.set('');
          this.added.set('');
          try {
            const group = await firstValueFrom(this.studentGroupApi.create({ name: this.newGroup().name }));
            this.added.set(group.name);
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
