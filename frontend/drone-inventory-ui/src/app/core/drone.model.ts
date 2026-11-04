/** Availability of a drone, derived by the backend from its active loan (REQ-002). */
export type DroneStatus = 'Available' | 'Loaned';

/** A drone as returned by GET /api/drones. */
export interface Drone {
  id: number;
  name: string;
  description: string;
  status: DroneStatus;
}

/** Body of POST /api/drones (REQ-001). */
export interface CreateDroneRequest {
  name: string;
  description?: string;
}
