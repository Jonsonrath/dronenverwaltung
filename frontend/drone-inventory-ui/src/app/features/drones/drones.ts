import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DroneApi } from '../../core/drone-api';

/** Drone list with the status derived by the backend (REQ-002). */
@Component({
  selector: 'app-drones',
  templateUrl: './drones.html',
  styleUrl: './drones.css',
})
export class Drones {
  private readonly droneApi = inject(DroneApi);

  protected readonly drones = rxResource({ stream: () => this.droneApi.list() });
}
