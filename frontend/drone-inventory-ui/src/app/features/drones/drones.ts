import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DroneApi } from '../../core/drone-api';
import { Drone } from '../../core/drone.model';
import { problemMessage } from '../../core/problem-message';
import { DroneGlyph } from '../../shared/drone-glyph';

/** Drone list with the derived status (REQ-002) and the forms to add (REQ-001) and edit (REQ-013) a drone. */
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

  protected readonly newDrone = signal({ name: '', description: '', ssid: '' });

  /** Message from the backend when adding failed, e.g. 409 duplicate name or 422 blank name. */
  protected readonly saveError = signal('');

  /** Name of the drone that was added last, for the confirmation text. */
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

  /** A click on the dimmed area around a sheet closes it; the dialog element itself is that area. */
  protected closeOnBackdrop(event: MouseEvent) {
    if (event.target instanceof HTMLDialogElement) {
      event.target.close();
    }
  }

  // ---- edit (REQ-013) ----

  private readonly editSheet = viewChild.required<ElementRef<HTMLDialogElement>>('editSheet');

  /** The drone whose name and description are being changed. */
  protected readonly editing = signal<Drone | undefined>(undefined);

  protected readonly changes = signal({ name: '', description: '', ssid: '' });

  /** Message from the backend when saving failed, e.g. 409 name of another drone. */
  protected readonly editError = signal('');

  /** Name of the drone that was saved last, for the confirmation text. */
  protected readonly saved = signal('');

  protected openEdit(drone: Drone) {
    this.editing.set(drone);
    this.editError.set('');
    this.editForm().reset({ name: drone.name, description: drone.description, ssid: drone.ssid ?? '' });
    this.editSheet().nativeElement.showModal();
  }

  protected closeEdit() {
    this.editSheet().nativeElement.close();
  }

  // Only a convenience check; the backend validates the name (REQ-013).
  protected readonly editForm = form(
    this.changes,
    (drone) => {
      required(drone.name, { message: 'Name eingeben.' });
    },
    {
      submission: {
        action: async () => {
          const drone = this.editing();
          if (!drone) {
            return undefined;
          }
          this.editError.set('');
          this.added.set('');
          this.saved.set('');
          try {
            const result = await firstValueFrom(this.droneApi.update(drone.id, this.changes()));
            this.saved.set(result.name);
            this.closeEdit();
            this.drones.reload();
          } catch (error) {
            this.editError.set(problemMessage(error));
          }
          return undefined;
        },
        onInvalid: () => this.editError.set(''),
      },
    },
  );

  // Only a convenience check; the backend validates the name (REQ-001).
  protected readonly droneForm = form(
    this.newDrone,
    (drone) => {
      required(drone.name, { message: 'Name eingeben.' });
    },
    {
      submission: {
        action: async () => {
          this.saveError.set('');
          this.added.set('');
          this.saved.set('');
          const { name, description, ssid } = this.newDrone();
          try {
            const drone = await firstValueFrom(this.droneApi.create({ name, description, ssid }));
            this.added.set(drone.name);
            this.closeSheet();
            this.droneForm().reset({ name: '', description: '', ssid: '' });
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
