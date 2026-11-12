/** Availability of a drone, derived by the backend from its active loan (REQ-002). */
export type DroneStatus = 'Available' | 'Loaned';

/** A drone as returned by GET /api/drones. */
export interface Drone {
  id: number;
  name: string;
  description: string;
  status: DroneStatus;
  /** Wi-Fi SSID, null when the drone has none (REQ-024). */
  ssid?: string | null;
}

/** Body of POST /api/drones (REQ-001, REQ-024). An empty SSID means none. */
export interface CreateDroneRequest {
  name: string;
  description?: string;
  ssid?: string;
}

/** Body of PUT /api/drones/{id} (REQ-013, REQ-024). An empty SSID removes it. */
export interface UpdateDroneRequest {
  name: string;
  description?: string;
  ssid?: string;
}
