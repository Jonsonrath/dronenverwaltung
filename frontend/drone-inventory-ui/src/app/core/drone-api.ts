import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateDroneRequest, Drone } from './drone.model';

/** Backend calls for drones (design.md §6). */
@Service()
export class DroneApi {
  private readonly http = inject(HttpClient);

  /** All drones with their derived status, ordered by name (REQ-002). */
  list(): Observable<Drone[]> {
    return this.http.get<Drone[]>('/api/drones');
  }

  /** Creates a drone (REQ-001). The backend rejects a blank name (422) or a duplicate (409). */
  create(request: CreateDroneRequest): Observable<Drone> {
    return this.http.post<Drone>('/api/drones', request);
  }
}
