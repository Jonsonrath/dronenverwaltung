import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DroneApi } from '../../core/drone-api';
import { problemMessage } from '../../core/problem-message';
import { DroneGlyph } from '../../shared/drone-glyph';

/** Drone list with the derived status (REQ-002) and the form to add a drone (REQ-001). */
@Component({
  selector: 'app-drones',
  imports: [FormField, FormRoot, RouterLink, DroneGlyph],
  templateUrl: './drones.html',
  styleUrl: './drones.css',
})
export class Drones {
  private readonly droneApi = inject(DroneApi);

  protected readonly drones = rxResource({ stream: () => this.droneApi.list() });

  /** How many of the listed drones the backend reports as Available – only counted, not decided here. */
  protected readonly availableCount = computed(() =>
    this.drones.hasValue() ? this.drones.value().filter((drone) => drone.status === 'Available').length : 0,
  );

  protected readonly newDrone = signal({ name: '', description: '' });

  /** Message from the backend when adding failed, e.g. 409 duplicate name or 422 blank name. */
  protected readonly saveError = signal('');

  /** Name of the drone that was added last, for the confirmation text. */
  protected readonly added = signal('');

  // Only a convenience check; the backend validates the name (REQ-001).
  protected readonly droneForm = form(
    this.newDrone,
    (drone) => {
      required(drone.name, { message: 'Enter a name.' });
    },
    {
      submission: {
        action: async () => {
          this.saveError.set('');
          this.added.set('');
          const { name, description } = this.newDrone();
          try {
            const drone = await firstValueFrom(this.droneApi.create({ name, description }));
            this.added.set(drone.name);
            this.droneForm().reset({ name: '', description: '' });
            this.drones.reload();
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
