import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Loan } from '../../core/loan.model';
import { StudentGroup } from '../../core/student-group.model';
import { StudentGroups } from './student-groups';

// jsdom has no dialog methods; the pages only need "open" and "closed".
HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
  this.open = true;
};
HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
  this.open = false;
};

// Task 6.3: student group page – list and form to add a group (REQ-003).
describe('Student groups page', () => {
  let fixture: ComponentFixture<StudentGroups>;
  let http: HttpTestingController;

  /** Let the async submit action run up to its next pending request. */
  const settle = () => new Promise((resolve) => setTimeout(resolve));

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StudentGroups],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(StudentGroups);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function respond(body: StudentGroup[] | null, status = 200, activeLoans: Partial<Loan>[] = []) {
    fixture.detectChanges();
    // Only for the badges "hat TELLO-02": the active loans.
    http.expectOne((r) => r.url === '/api/loans' && r.params.get('status') === 'Active').flush(activeLoans);
    const request = http.expectOne('/api/student-groups');
    if (status === 200) {
      request.flush(body);
    } else {
      request.flush(null, { status, statusText: 'Error' });
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  async function submitName(page: HTMLElement, name: string) {
    const input = page.querySelector('#group-name') as HTMLInputElement;
    input.value = name;
    input.dispatchEvent(new Event('input'));
    page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await settle();
    fixture.detectChanges();
  }

  async function answerPost(body: object, status: number) {
    http.expectOne({ method: 'POST', url: '/api/student-groups' })
      .flush(body, { status, statusText: status === 201 ? 'Created' : 'Error' });
    await settle();
    fixture.detectChanges();
  }

  const names = (page: HTMLElement) => [...page.querySelectorAll('.groups li .name')].map((name) => name.textContent?.trim());

  it('REQ-003: lists the student groups', async () => {
    const page = await respond([
      { id: 1, name: 'Group 1' },
      { id: 3, name: 'Group 3' },
    ]);

    expect(names(page)).toEqual(['Group 1', 'Group 3']);
  });

  it('shows which drone a group has right now, read from the active loans', async () => {
    const page = await respond(
      [
        { id: 1, name: 'Group 1' },
        { id: 3, name: 'Group 3' },
      ],
      200,
      [{ id: 5, studentGroupId: 3, droneName: 'TELLO-02', status: 'Active' }],
    );

    const badges = [...page.querySelectorAll('.groups li')].map(
      (li) => li.querySelector('.has-drone, .no-drone')?.textContent?.trim(),
    );
    expect(badges).toEqual(['keine Drohne', 'hat TELLO-02']);
  });

  it('REQ-003: shows a message when there are no student groups', async () => {
    const page = await respond([]);

    expect(page.querySelector('.empty')?.textContent).toContain('Noch keine Schülergruppen.');
  });

  it('shows an error with a retry button when the backend fails', async () => {
    const page = await respond(null, 500);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('konnten nicht geladen werden');
  });

  it('opens the form as a sheet from the button in the page head', async () => {
    const page = await respond([]);
    const sheet = page.querySelector('dialog') as HTMLDialogElement;
    expect(sheet.open).toBe(false);

    (page.querySelector('.page-head button') as HTMLButtonElement).click();

    expect(sheet.open).toBe(true);
    expect(sheet.querySelector('#group-name')).not.toBeNull();
  });

  it('REQ-003: a new group is sent to the backend and appears in the list', async () => {
    const page = await respond([{ id: 1, name: 'Group 1' }]);
    (page.querySelector('.page-head button') as HTMLButtonElement).click();

    await submitName(page, 'Group 3');
    const create = http.expectOne({ method: 'POST', url: '/api/student-groups' });
    expect(create.request.body).toEqual({ name: 'Group 3' });
    create.flush({ id: 3, name: 'Group 3' }, { status: 201, statusText: 'Created' });
    await settle();
    fixture.detectChanges(); // the reload request is sent on the next change detection
    http.expectOne({ method: 'GET', url: '/api/student-groups' }).flush([
      { id: 1, name: 'Group 1' },
      { id: 3, name: 'Group 3' },
    ]);
    await settle();
    fixture.detectChanges();

    expect(names(page)).toEqual(['Group 1', 'Group 3']);
    expect(page.querySelector('[role="status"]')?.textContent).toContain('Group 3 hinzugefügt.');
    // The sheet closes after the group was added.
    expect((page.querySelector('dialog') as HTMLDialogElement).open).toBe(false);
    expect((page.querySelector('#group-name') as HTMLInputElement).value).toBe('');
  });

  it('REQ-003: shows the backend message for a blank name (422)', async () => {
    const page = await respond([]);

    await submitName(page, '   ');
    await answerPost({ status: 422, detail: 'Der Name einer Schülergruppe darf nicht leer sein.' }, 422);

    expect(page.querySelector('form [role="alert"]')?.textContent).toContain(
      'Der Name einer Schülergruppe darf nicht leer sein.',
    );
    http.expectNone({ method: 'GET', url: '/api/student-groups' });
  });

  it('REQ-003: shows the backend message for a duplicate name (409)', async () => {
    const page = await respond([{ id: 3, name: 'Group 3' }]);

    await submitName(page, ' group 3 ');
    await answerPost(
      { status: 409, detail: 'Eine Schülergruppe mit dem Namen "group 3" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).' },
      409,
    );

    expect(page.querySelector('form [role="alert"]')?.textContent).toContain('gibt es bereits');
  });

  it('does not send an empty name and asks for one', async () => {
    const page = await respond([]);

    await submitName(page, '');

    http.expectNone({ method: 'POST', url: '/api/student-groups' });
    expect(page.querySelector('.field-error')?.textContent).toContain('Name eingeben.');
  });
});
