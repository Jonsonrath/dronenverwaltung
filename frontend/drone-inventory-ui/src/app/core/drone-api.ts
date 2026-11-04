import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { Drone } from './drone.model';

/** Backend calls for drones (design.md §6). */
@Service()
export class DroneApi {
  private readonly http = inject(HttpClient);

  /** All drones with their derived status, ordered by name (REQ-002). */
  list(): Observable<Drone[]> {
    return this.http.get<Drone[]>('/api/drones');
  }
}
