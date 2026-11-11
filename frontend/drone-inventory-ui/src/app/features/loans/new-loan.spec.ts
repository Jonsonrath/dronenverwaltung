import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Drone } from '../../core/drone.model';
import { Loan } from '../../core/loan.model';
import { StudentGroup } from '../../core/student-group.model';
import { NewLoan } from './new-loan';

// Loan form (REQ-004, REQ-005, REQ-006): task 7.1; drone rows, group chips and steppers: improve-frontend-design task 3.2.
describe('New loan page', () => {
  let fixture: ComponentFixture<NewLoan>;
  let http: HttpTestingController;

  const drones: Drone[] = [
    { id: 2, name: 'TELLO-02', description: '', status: 'Loaned' },
    { id: 7, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', status: 'Available' },
  ];
  const groups: StudentGroup[] = [
    { id: 1, name: 'Group 1' },
    { id: 3, name: 'Group 3' },
  ];

  /** Let the async submit action run up to its next pending request. */
  const settle = () => new Promise((resolve) => setTimeout(resolve));

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [NewLoan],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    fixture = TestBed.createComponent(NewLoan);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function load(droneList = drones, groupList = groups) {
    fixture.detectChanges();
    http.expectOne('/api/drones').flush(droneList);
    http.expectOne('/api/student-groups').flush(groupList);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  /** Tap the drone row or group chip with this id (radio inputs styled as rows and chips). */
  function pick(page: HTMLElement, list: 'drones' | 'groups', id: string) {
    const radio = page.querySelector(`fieldset.${list} input[value="${id}"]`) as HTMLInputElement;
    radio.click();
  }

  /** Type a quantity into a stepper's number field. */
  function type(page: HTMLElement, id: string, value: string) {
    const field = page.querySelector(`#${id}`) as HTMLInputElement;
    field.value = value;
    field.dispatchEvent(new Event('input'));
  }

  /** Names shown in one group of the drone choice ('available' or 'loaned'). */
  const droneNames = (page: HTMLElement, group: string) =>
    [...page.querySelectorAll(`.drone-group[data-group="${group}"] .name`)].map((n) => n.textContent);

  async function submit(page: HTMLElement) {
    page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await settle();
    fixture.detectChanges();
  }

  async function answerPost(body: object, status: number) {
    http.expectOne({ method: 'POST', url: '/api/loans' })
      .flush(body, { status, statusText: status === 201 ? 'Created' : 'Error' });
    await settle();
    fixture.detectChanges();
  }

  it('REQ-004/REQ-006: loans the chosen drone with batteries and propellers', async () => {
    const page = await load();
    pick(page, 'drones', '7');
    pick(page, 'groups', '3');
    type(page, 'loan-batteries', '4');
    type(page, 'loan-propellers', '8');

    await submit(page);
    const create = http.expectOne({ method: 'POST', url: '/api/loans' });
    expect(create.request.body).toEqual({ droneId: 7, studentGroupId: 3, batteries: 4, propellers: 8 });
    const loan: Loan = {
      id: 1, droneId: 7, droneName: 'TELLO-07', studentGroupId: 3, studentGroupName: 'Group 3',
      batteries: 4, propellers: 8, loanDate: '2026-08-19T08:00:00Z', returnDate: null, status: 'Active',
    };
    create.flush(loan, { status: 201, statusText: 'Created' });
    await settle();
    fixture.detectChanges();

    // The drone list is loaded again, so TELLO-07 moves to "Loaned right now".
    http.expectOne({ method: 'GET', url: '/api/drones' }).flush([
      drones[0],
      { ...drones[1], status: 'Loaned' },
    ]);
    await settle();
    fixture.detectChanges();

    expect(page.querySelector('[role="status"]')?.textContent).toContain(
      'TELLO-07 loaned to Group 3 with 4 batteries and 8 propellers.',
    );
    expect(droneNames(page, 'loaned')).toEqual(['TELLO-02', 'TELLO-07']);
    expect(page.querySelector('fieldset.drones input:checked')).toBeNull();
  });

  it('REQ-006: batteries and propellers can be counted with the + buttons only', async () => {
    const page = await load();
    pick(page, 'drones', '7');
    pick(page, 'groups', '3');
    for (let i = 0; i < 4; i++) {
      (page.querySelector('button[aria-label="More batteries"]') as HTMLButtonElement).click();
    }
    (page.querySelector('button[aria-label="More propellers"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    await submit(page);
    const create = http.expectOne({ method: 'POST', url: '/api/loans' });

    expect(create.request.body).toEqual({ droneId: 7, studentGroupId: 3, batteries: 4, propellers: 1 });
    create.flush({}, { status: 201, statusText: 'Created' });
    await settle();
    fixture.detectChanges();
    http.expectOne({ method: 'GET', url: '/api/drones' }).flush(drones);
  });

  it('REQ-006: missing accessories are sent as 0', async () => {
    const page = await load();
    pick(page, 'drones', '7');
    pick(page, 'groups', '3');

    await submit(page);
    const create = http.expectOne({ method: 'POST', url: '/api/loans' });

    expect(create.request.body).toEqual({ droneId: 7, studentGroupId: 3, batteries: 0, propellers: 0 });
    create.flush({}, { status: 201, statusText: 'Created' });
    await settle();
    fixture.detectChanges();
    http.expectOne({ method: 'GET', url: '/api/drones' }).flush(drones);
  });

  it('REQ-005: shows the backend message when the drone is already loaned (409)', async () => {
    const page = await load();
    pick(page, 'drones', '2');
    pick(page, 'groups', '1');

    await submit(page);
    await answerPost({ status: 409, title: 'Conflict', detail: 'Drone "TELLO-02" already has an active loan.' }, 409);

    expect(page.querySelector('form [role="alert"]')?.textContent).toContain(
      'Drone "TELLO-02" already has an active loan.',
    );
    expect(page.querySelector('[role="status"]')).toBeNull();
  });

  it('REQ-006: shows the backend message for a negative quantity (422)', async () => {
    const page = await load();
    pick(page, 'drones', '7');
    pick(page, 'groups', '3');
    type(page, 'loan-batteries', '-1');

    await submit(page);
    await answerPost({ status: 422, detail: 'The number of batteries must not be negative.' }, 422);

    expect(page.querySelector('form [role="alert"]')?.textContent).toContain(
      'The number of batteries must not be negative.',
    );
  });

  it('REQ-005: groups the drones by the status from the backend', async () => {
    const page = await load();

    expect(droneNames(page, 'available')).toEqual(['TELLO-07']);
    expect(droneNames(page, 'loaned')).toEqual(['TELLO-02']);
  });

  it('asks for a drone and a group before sending anything', async () => {
    const page = await load();

    await submit(page);

    http.expectNone({ method: 'POST', url: '/api/loans' });
    const hints = [...page.querySelectorAll('.field-error')].map((e) => e.textContent);
    expect(hints).toEqual(['Choose a drone.', 'Choose a student group.']);
  });

  it('points to the other pages when there are no drones or groups yet', async () => {
    const page = await load([], []);

    expect(page.querySelector('form')).toBeNull();
    expect(page.querySelector('a[href="/drones"]')?.textContent).toBe('Add a drone');
    expect(page.querySelector('a[href="/student-groups"]')?.textContent).toBe('Add a student group');
  });

  it('reads the choice back on the loan slip before it is sent', async () => {
    const page = await load();
    const slip = () => [...page.querySelectorAll('.slip dd')].map((value) => value.textContent?.trim());
    expect(slip()).toEqual(['Not chosen yet', 'Not chosen yet', '0', '0']);

    pick(page, 'drones', '7');
    pick(page, 'groups', '3');
    type(page, 'loan-batteries', '4');
    fixture.detectChanges();

    expect(slip()).toEqual(['TELLO-07', 'Group 3', '4', '0']);
  });
});

// Shortcut "Loan this drone" from a bay of the drones page: /loans/new?drone=<id>.
describe('New loan page opened from a drone bay', () => {
  let http: HttpTestingController;

  async function open(drone: string) {
    TestBed.configureTestingModule({
      imports: [NewLoan],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ drone }) } } },
      ],
    });
    const fixture = TestBed.createComponent(NewLoan);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/drones').flush([
      { id: 2, name: 'TELLO-02', description: '', status: 'Loaned' },
      { id: 7, name: 'TELLO-07', description: '', status: 'Available' },
    ]);
    http.expectOne('/api/student-groups').flush([{ id: 3, name: 'Group 3' }]);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  afterEach(() => http.verify());

  it('has the drone of the link chosen already', async () => {
    const page = await open('7');

    expect((page.querySelector('fieldset.drones input:checked') as HTMLInputElement | null)?.value).toBe('7');
    expect(page.querySelector('.slip dd')?.textContent?.trim()).toBe('TELLO-07');
  });

  it('chooses nothing when the backend does not list that drone', async () => {
    const page = await open('99');

    expect(page.querySelector('fieldset.drones input:checked')).toBeNull();
    expect(page.querySelector('.slip dd')?.textContent?.trim()).toBe('Not chosen yet');
  });
});
