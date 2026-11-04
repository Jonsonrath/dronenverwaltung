import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Drone } from '../../core/drone.model';
import { Drones } from './drones';

// Task 6.1: drone list page (REQ-002).
describe('Drones page', () => {
  let fixture: ComponentFixture<Drones>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Drones],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(Drones);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function respond(body: Drone[] | null, status = 200) {
    fixture.detectChanges();
    const request = http.expectOne('/api/drones');
    if (status === 200) {
      request.flush(body);
    } else {
      request.flush(null, { status, statusText: 'Error' });
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('REQ-002: lists drones with name, status and description', async () => {
    const page = await respond([
      { id: 1, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 2, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt', status: 'Available' },
    ]);

    const rows = [...page.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['TELLO-02', 'Loaned', ''],
      ['TELLO-07', 'Available', 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt'],
    ]);
    expect(page.querySelector('tbody tr:first-child .tag')?.classList).toContain('tag-loaned');
    expect(page.querySelector('tbody tr:last-child .tag')?.classList).toContain('tag-available');
  });

  it('REQ-002: shows a message when there are no drones', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('No drones yet.');
    expect(page.querySelector('table')).toBeNull();
  });

  it('shows an error with a retry button when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');

    (page.querySelector('[role="alert"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    http.expectOne('/api/drones').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.querySelector('.empty')?.textContent).toContain('No drones yet.');
  });
});
