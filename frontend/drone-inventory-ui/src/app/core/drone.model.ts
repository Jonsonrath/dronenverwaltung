/**
 * Status of a drone, derived by the backend (REQ-002): Loaned with an active loan,
 * otherwise Damaged if a damage is reported, otherwise Available.
 */
export type DroneStatus = 'Available' | 'Loaned' | 'Damaged';

/** A drone as returned by GET /api/drones. */
export interface Drone {
  id: number;
  name: string;
  description: string;
  status: DroneStatus;
  /** Reason of the reported damage (REQ-014); null or missing when the drone is not damaged. */
  damageReason?: string | null;
}

/** Body of POST /api/drones (REQ-001). */
export interface CreateDroneRequest {
  name: string;
  description?: string;
}

/** Body of PUT /api/drones/{id} (REQ-013). */
export interface UpdateDroneRequest {
  name: string;
  description?: string;
}

/** Body of POST /api/drones/{id}/damage (REQ-014). */
export interface ReportDamageRequest {
  reason: string;
}
