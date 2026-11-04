import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { DroneApi } from './drone-api';
import { Drone } from './drone.model';

// Task 6.1: DroneApi calls the backend (REQ-002).
describe('DroneApi', () => {
  let api: DroneApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(DroneApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('REQ-002: list() sends GET /api/drones and returns the drones', async () => {
    const drones: Drone[] = [
      { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', status: 'Available' },
    ];

    const result = firstValueFrom(api.list());
    const request = http.expectOne('/api/drones');
    request.flush(drones);

    expect(request.request.method).toBe('GET');
    expect(await result).toEqual(drones);
  });
});
