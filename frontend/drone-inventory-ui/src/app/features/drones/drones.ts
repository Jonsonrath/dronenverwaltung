import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DroneApi } from '../../core/drone-api';
import { Drone } from '../../core/drone.model';
import { problemMessage } from '../../core/problem-message';
import { DroneGlyph } from '../../shared/drone-glyph';

/**
 * Drone list with the derived status (REQ-002), the forms to add (REQ-001) and edit (REQ-013) a drone,
 * and reporting damage (REQ-014) / repair (REQ-015).
 */
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
          this.clearNotes();
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

  // ---- damage (REQ-014) and repair (REQ-015) ----

  private readonly damageSheet = viewChild.required<ElementRef<HTMLDialogElement>>('damageSheet');

  /** The drone that is being reported as damaged. */
  protected readonly reporting = signal<Drone | undefined>(undefined);

  protected readonly damage = signal({ reason: '' });

  /** Message from the backend when reporting failed, e.g. 422 empty reason or 404 unknown drone. */
  protected readonly damageError = signal('');

  /** Message from the backend when a repair failed, e.g. 409 not damaged or 404 unknown drone. */
  protected readonly repairError = signal('');

  /** Confirmation after a damage report or repair. */
  protected readonly damageNote = signal('');

  protected openDamage(drone: Drone) {
    this.reporting.set(drone);
    this.damageError.set('');
    this.damageForm().reset({ reason: drone.damageReason ?? '' });
    this.damageSheet().nativeElement.showModal();
  }

  protected closeDamage() {
    this.damageSheet().nativeElement.close();
  }

  private clearNotes() {
    this.added.set('');
    this.saved.set('');
    this.damageNote.set('');
    this.repairError.set('');
  }

  // Only a convenience check; the backend validates the reason (REQ-014).
  protected readonly damageForm = form(
    this.damage,
    (damage) => {
      required(damage.reason, { message: 'Grund eingeben.' });
    },
    {
      submission: {
        action: async () => {
          const drone = this.reporting();
          if (!drone) {
            return undefined;
          }
          this.damageError.set('');
          this.clearNotes();
          try {
            const result = await firstValueFrom(this.droneApi.reportDamage(drone.id, this.damage()));
            this.damageNote.set(`${result.name} als defekt gemeldet.`);
            this.closeDamage();
            this.drones.reload();
          } catch (error) {
            this.damageError.set(problemMessage(error));
          }
          return undefined;
        },
        onInvalid: () => this.damageError.set(''),
      },
    },
  );

  /** Ids of the drones whose repair request is running, so the button cannot be pressed twice. */
  protected readonly repairing = signal<readonly number[]>([]);

  protected async repair(drone: Drone) {
    this.clearNotes();
    this.repairing.update((ids) => [...ids, drone.id]);
    try {
      const result = await firstValueFrom(this.droneApi.repair(drone.id));
      this.damageNote.set(`${result.name} ist repariert.`);
      this.drones.reload();
    } catch (error) {
      this.repairError.set(problemMessage(error));
    } finally {
      this.repairing.update((ids) => ids.filter((id) => id !== drone.id));
    }
  }

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
          this.clearNotes();
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
