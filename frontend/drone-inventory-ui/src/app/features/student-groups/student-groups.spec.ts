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

  describe('drone badges before the active loans are known', () => {
    const groups: StudentGroup[] = [{ id: 1, name: 'Group 1' }];
    const loansRequest = () => http.expectOne((r) => r.url === '/api/loans' && r.params.get('status') === 'Active');
    const badges = (page: HTMLElement) => page.querySelectorAll('.groups li .has-drone, .groups li .no-drone').length;

    async function groupsOnly() {
      fixture.detectChanges();
      http.expectOne('/api/student-groups').flush(groups);
      await settle();
      fixture.detectChanges();
      return fixture.nativeElement as HTMLElement;
    }

    it('shows no badge while the active loans are still loading', async () => {
      const page = await groupsOnly();

      expect(page.querySelector('.groups li .name')?.textContent?.trim()).toBe('Group 1');
      expect(badges(page)).toBe(0);

      // Once they arrive, the group gets its badge.
      loansRequest().flush([]);
      await settle();
      fixture.detectChanges();
      expect(page.querySelector('.groups li .no-drone')?.textContent?.trim()).toBe('keine Drohne');
    });

    it('shows no badge when the active loans could not be loaded', async () => {
      const page = await groupsOnly();

      loansRequest().flush(null, { status: 500, statusText: 'Error' });
      await settle();
      fixture.detectChanges();

      expect(page.querySelector('.groups li .name')?.textContent?.trim()).toBe('Group 1');
      expect(badges(page)).toBe(0);
    });
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

  // Change manage-student-groups: rename (REQ-011) and delete (REQ-012) from the group's plate.
  describe('rename and delete', () => {
    const groups: StudentGroup[] = [
      { id: 1, name: 'Group 1' },
      { id: 3, name: 'Grupe 3' },
    ];

    const sheet = (page: HTMLElement, heading: string) =>
      page.querySelector(`dialog[aria-labelledby="${heading}"]`) as HTMLDialogElement;

    function click(page: HTMLElement, label: string) {
      (page.querySelector(`button[aria-label="${label}"]`) as HTMLButtonElement).click();
      fixture.detectChanges();
    }

    async function submitNewName(page: HTMLElement, name: string) {
      const input = page.querySelector('#group-new-name') as HTMLInputElement;
      input.value = name;
      input.dispatchEvent(new Event('input'));
      page.querySelector('form.rename-form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    async function reloadWith(body: StudentGroup[]) {
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/student-groups' }).flush(body);
      await settle();
      fixture.detectChanges();
    }

    it('REQ-011: opens the rename sheet with the current name', async () => {
      const page = await respond(groups);

      click(page, 'Grupe 3 umbenennen');

      const rename = sheet(page, 'rename-group-heading');
      expect(rename.open).toBe(true);
      expect(rename.querySelector('h2')?.textContent).toBe('Grupe 3 umbenennen');
      expect((page.querySelector('#group-new-name') as HTMLInputElement).value).toBe('Grupe 3');
    });

    it('REQ-011: sends the new name to the backend and shows the renamed group', async () => {
      const page = await respond(groups);
      click(page, 'Grupe 3 umbenennen');

      await submitNewName(page, 'Group 3');
      const request = http.expectOne({ method: 'PUT', url: '/api/student-groups/3' });
      expect(request.request.body).toEqual({ name: 'Group 3' });
      request.flush({ id: 3, name: 'Group 3' });
      await reloadWith([groups[0], { id: 3, name: 'Group 3' }]);

      expect(names(page)).toEqual(['Group 1', 'Group 3']);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Grupe 3 heißt jetzt Group 3.');
      expect(sheet(page, 'rename-group-heading').open).toBe(false);
    });

    it('REQ-011: shows the backend message when another group has the name (409)', async () => {
      const page = await respond(groups);
      click(page, 'Grupe 3 umbenennen');

      await submitNewName(page, ' group 1 ');
      http.expectOne({ method: 'PUT', url: '/api/student-groups/3' }).flush(
        { status: 409, detail: 'Eine Schülergruppe mit dem Namen "group 1" gibt es bereits.' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      const rename = sheet(page, 'rename-group-heading');
      expect(rename.querySelector('[role="alert"]')?.textContent).toContain('gibt es bereits');
      expect(rename.open).toBe(true);
      expect(names(page)).toEqual(['Group 1', 'Grupe 3']);
      http.expectNone({ method: 'GET', url: '/api/student-groups' });
    });

    it('REQ-012: deletes a group after the confirmation and removes it from the list', async () => {
      const page = await respond(groups);

      click(page, 'Grupe 3 löschen');
      const confirm = sheet(page, 'delete-group-heading');
      expect(confirm.open).toBe(true);
      expect(confirm.querySelector('h2')?.textContent).toBe('Grupe 3 löschen?');
      // Nothing is sent before the confirmation.
      http.expectNone({ method: 'DELETE', url: '/api/student-groups/3' });

      (confirm.querySelector('.confirm-delete') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'DELETE', url: '/api/student-groups/3' }).flush(null, { status: 204, statusText: 'No Content' });
      await reloadWith([groups[0]]);

      expect(names(page)).toEqual(['Group 1']);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Grupe 3 gelöscht.');
      expect(confirm.open).toBe(false);
    });

    it('REQ-012: shows the backend message when the group has loans (409) and keeps the group', async () => {
      const page = await respond(groups);
      click(page, 'Grupe 3 löschen');
      const confirm = sheet(page, 'delete-group-heading');

      (confirm.querySelector('.confirm-delete') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'DELETE', url: '/api/student-groups/3' }).flush(
        { status: 409, detail: 'Die Schülergruppe "Grupe 3" hat Ausleihen und kann nicht gelöscht werden.' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      expect(confirm.querySelector('[role="alert"]')?.textContent).toContain('hat Ausleihen und kann nicht gelöscht werden');
      expect(confirm.open).toBe(true);
      expect(names(page)).toEqual(['Group 1', 'Grupe 3']);
      http.expectNone({ method: 'GET', url: '/api/student-groups' });
    });

    it('does not send anything when the confirmation is cancelled', async () => {
      const page = await respond(groups);
      click(page, 'Grupe 3 löschen');
      const confirm = sheet(page, 'delete-group-heading');

      (confirm.querySelector('.sheet-actions .button') as HTMLButtonElement).click();

      expect(confirm.open).toBe(false);
      http.expectNone({ method: 'DELETE', url: '/api/student-groups/3' });
    });
  });

  // Change student-group-members: members on the plates (REQ-017) and adding a student (REQ-016).
  describe('students', () => {
    const groups: StudentGroup[] = [
      {
        id: 3,
        name: 'Group 3',
        students: [
          { id: 7, name: 'Anna Berger', class: '4AHIF', studentGroupId: 3 },
          { id: 8, name: 'Ben Huber', class: '', studentGroupId: 3 },
        ],
      },
      { id: 86, name: 'Group 86', students: [] },
    ];

    const plate = (page: HTMLElement, name: string) =>
      [...page.querySelectorAll('.groups > li')].find((li) => li.querySelector('.name')?.textContent?.trim() === name)!;

    const members = (page: HTMLElement, name: string) =>
      [...plate(page, name).querySelectorAll('.member')].map((member) =>
        [member.querySelector('.member-name')?.textContent?.trim(), member.querySelector('.member-class')?.textContent?.trim()],
      );

    const studentSheet = (page: HTMLElement) =>
      page.querySelector('dialog[aria-labelledby="add-student-heading"]') as HTMLDialogElement;

    function openAddStudent(page: HTMLElement, groupName: string) {
      (page.querySelector(`button[aria-label="Schüler zu ${groupName} hinzufügen"]`) as HTMLButtonElement).click();
      fixture.detectChanges();
    }

    async function submitStudent(page: HTMLElement, name: string, schoolClass: string) {
      const nameInput = page.querySelector('#student-name') as HTMLInputElement;
      nameInput.value = name;
      nameInput.dispatchEvent(new Event('input'));
      const classInput = page.querySelector('#student-class') as HTMLInputElement;
      classInput.value = schoolClass;
      classInput.dispatchEvent(new Event('input'));
      page.querySelector('form.student-form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await settle();
      fixture.detectChanges();
    }

    it('REQ-017: shows the members of every group with their class, in the order of the backend', async () => {
      const page = await respond(groups);

      expect(members(page, 'Group 3')).toEqual([
        ['Anna Berger', '4AHIF'],
        ['Ben Huber', undefined],
      ]);
      expect(plate(page, 'Group 86').querySelector('.no-members')?.textContent).toContain('Noch keine Schüler.');
    });

    it('REQ-016: opens the sheet for the group of the plate', async () => {
      const page = await respond(groups);

      openAddStudent(page, 'Group 86');

      const sheet = studentSheet(page);
      expect(sheet.open).toBe(true);
      expect(sheet.querySelector('h2')?.textContent).toBe('Schüler zu Group 86 hinzufügen');
    });

    it('REQ-016: sends name and class to the backend and shows the reloaded members', async () => {
      const page = await respond(groups);
      openAddStudent(page, 'Group 86');

      await submitStudent(page, 'Clara Wolf', '4AHIF');
      const request = http.expectOne({ method: 'POST', url: '/api/student-groups/86/students' });
      expect(request.request.body).toEqual({ name: 'Clara Wolf', class: '4AHIF' });
      request.flush({ id: 9, name: 'Clara Wolf', class: '4AHIF', studentGroupId: 86 }, { status: 201, statusText: 'Created' });
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/student-groups' }).flush([
        groups[0],
        { id: 86, name: 'Group 86', students: [{ id: 9, name: 'Clara Wolf', class: '4AHIF', studentGroupId: 86 }] },
      ]);
      await settle();
      fixture.detectChanges();

      expect(members(page, 'Group 86')).toEqual([['Clara Wolf', '4AHIF']]);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Clara Wolf ist jetzt in Group 86.');
      expect(studentSheet(page).open).toBe(false);
    });

    it('REQ-016: shows the backend message when the group already has the name (409)', async () => {
      const page = await respond(groups);
      openAddStudent(page, 'Group 3');

      await submitStudent(page, ' anna berger ', '');
      http.expectOne({ method: 'POST', url: '/api/student-groups/3/students' }).flush(
        { status: 409, detail: 'In der Gruppe "Group 3" gibt es bereits einen Schüler "anna berger".' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      const sheet = studentSheet(page);
      expect(sheet.querySelector('[role="alert"]')?.textContent).toContain('gibt es bereits einen Schüler');
      expect(sheet.open).toBe(true);
      http.expectNone({ method: 'GET', url: '/api/student-groups' });
    });

    it('REQ-016: shows the backend message for a blank name (422)', async () => {
      const page = await respond(groups);
      openAddStudent(page, 'Group 3');

      // Only spaces pass the convenience check; the backend decides.
      await submitStudent(page, '   ', '');
      http.expectOne({ method: 'POST', url: '/api/student-groups/3/students' }).flush(
        { status: 422, detail: 'Der Name eines Schülers darf nicht leer sein.' },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
      await settle();
      fixture.detectChanges();

      expect(studentSheet(page).querySelector('[role="alert"]')?.textContent).toContain(
        'Der Name eines Schülers darf nicht leer sein.',
      );
      http.expectNone({ method: 'GET', url: '/api/student-groups' });
    });

    it('does not send an empty student name and asks for one', async () => {
      const page = await respond(groups);
      openAddStudent(page, 'Group 3');

      await submitStudent(page, '', '4AHIF');

      http.expectNone({ method: 'POST', url: '/api/student-groups/3/students' });
      expect(studentSheet(page).querySelector('.field-error')?.textContent).toContain('Name eingeben.');
    });
  });

  // Change student-group-members: move (REQ-018) and remove (REQ-019) a student from its sheet.
  describe('move and remove a student', () => {
    const anna = { id: 7, name: 'Anna Berger', class: '4AHIF', studentGroupId: 3 };
    const groups: StudentGroup[] = [
      { id: 1, name: 'Group 1', students: [] },
      { id: 3, name: 'Group 3', students: [anna] },
      { id: 86, name: 'Group 86', students: [] },
    ];

    const sheet = (page: HTMLElement) => page.querySelector('dialog[aria-labelledby="student-heading"]') as HTMLDialogElement;

    const memberNames = (page: HTMLElement, groupName: string) =>
      [
        ...[...page.querySelectorAll('.groups > li')]
          .find((li) => li.querySelector('.name')?.textContent?.trim() === groupName)!
          .querySelectorAll('.member-name'),
      ].map((name) => name.textContent?.trim());

    function openAnna(page: HTMLElement) {
      (page.querySelector('button[aria-label="Anna Berger bearbeiten"]') as HTMLButtonElement).click();
      fixture.detectChanges();
    }

    const chip = (page: HTMLElement, name: string) =>
      [...sheet(page).querySelectorAll('.target-chip')].find((c) => c.textContent?.trim() === name) as HTMLButtonElement;

    async function reloadWith(body: StudentGroup[]) {
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/student-groups' }).flush(body);
      await settle();
      fixture.detectChanges();
    }

    it('REQ-018: opens the sheet with the other groups as targets', async () => {
      const page = await respond(groups);

      openAnna(page);

      expect(sheet(page).open).toBe(true);
      expect(sheet(page).querySelector('h2')?.textContent).toBe('Anna Berger');
      expect([...sheet(page).querySelectorAll('.target-chip')].map((c) => c.textContent?.trim())).toEqual([
        'Group 1',
        'Group 86',
      ]);
    });

    it('REQ-018: moves the student to the chosen group and shows the reloaded list', async () => {
      const page = await respond(groups);
      openAnna(page);

      chip(page, 'Group 1').click();
      await settle();
      const request = http.expectOne({ method: 'PUT', url: '/api/students/7/group' });
      expect(request.request.body).toEqual({ studentGroupId: 1 });
      request.flush({ ...anna, studentGroupId: 1 });
      await reloadWith([
        { id: 1, name: 'Group 1', students: [{ ...anna, studentGroupId: 1 }] },
        { id: 3, name: 'Group 3', students: [] },
        groups[2],
      ]);

      expect(memberNames(page, 'Group 1')).toEqual(['Anna Berger']);
      expect(memberNames(page, 'Group 3')).toEqual([]);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Anna Berger ist jetzt in Group 1.');
      expect(sheet(page).open).toBe(false);
    });

    it('REQ-018: shows the backend message when the target group has the name (409) and keeps the student', async () => {
      const page = await respond(groups);
      openAnna(page);

      chip(page, 'Group 1').click();
      await settle();
      http.expectOne({ method: 'PUT', url: '/api/students/7/group' }).flush(
        { status: 409, detail: 'In der Gruppe "Group 1" gibt es bereits einen Schüler "Anna Berger".' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      expect(sheet(page).querySelector('[role="alert"]')?.textContent).toContain('gibt es bereits einen Schüler');
      expect(sheet(page).open).toBe(true);
      expect(memberNames(page, 'Group 3')).toEqual(['Anna Berger']);
      http.expectNone({ method: 'GET', url: '/api/student-groups' });
    });

    it('REQ-019: removes the student and shows the group without it', async () => {
      const page = await respond(groups);
      openAnna(page);

      (sheet(page).querySelector('.remove-student') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'DELETE', url: '/api/students/7' }).flush(null, { status: 204, statusText: 'No Content' });
      await reloadWith([groups[0], { id: 3, name: 'Group 3', students: [] }, groups[2]]);

      expect(memberNames(page, 'Group 3')).toEqual([]);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Anna Berger aus Group 3 entfernt.');
      expect(sheet(page).open).toBe(false);
    });

    it('REQ-019: shows the backend message when the student no longer exists (404)', async () => {
      const page = await respond(groups);
      openAnna(page);

      (sheet(page).querySelector('.remove-student') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'DELETE', url: '/api/students/7' }).flush(
        { status: 404, detail: 'Den Schüler 7 gibt es nicht.' },
        { status: 404, statusText: 'Not Found' },
      );
      await settle();
      fixture.detectChanges();

      expect(sheet(page).querySelector('[role="alert"]')?.textContent).toContain('Den Schüler 7 gibt es nicht.');
    });

    it('REQ-012: shows the backend message when a group with students is deleted (409)', async () => {
      const page = await respond(groups);
      (page.querySelector('button[aria-label="Group 3 löschen"]') as HTMLButtonElement).click();
      fixture.detectChanges();
      const confirm = page.querySelector('dialog[aria-labelledby="delete-group-heading"]') as HTMLDialogElement;

      (confirm.querySelector('.confirm-delete') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'DELETE', url: '/api/student-groups/3' }).flush(
        { status: 409, detail: 'Die Schülergruppe "Group 3" hat noch Schüler und kann nicht gelöscht werden.' },
        { status: 409, statusText: 'Conflict' },
      );
      await settle();
      fixture.detectChanges();

      expect(confirm.querySelector('[role="alert"]')?.textContent).toContain('hat noch Schüler');
      expect(memberNames(page, 'Group 3')).toEqual(['Anna Berger']);
    });

    it('does not send anything when the sheet is cancelled', async () => {
      const page = await respond(groups);
      openAnna(page);

      (sheet(page).querySelector('.sheet-actions .button') as HTMLButtonElement).click();

      expect(sheet(page).open).toBe(false);
      http.expectNone({ method: 'PUT', url: '/api/students/7/group' });
      http.expectNone({ method: 'DELETE', url: '/api/students/7' });
    });
  });

  // Change responsible-student: mark, change and clear the responsible student (REQ-020).
  describe('responsible student', () => {
    const anna = { id: 7, name: 'Anna Berger', class: '', studentGroupId: 3 };
    const ben = { id: 8, name: 'Ben Huber', class: '', studentGroupId: 3 };
    const group3 = (responsibleStudentId: number | null): StudentGroup => ({
      id: 3,
      name: 'Group 3',
      students: [anna, ben],
      responsibleStudentId,
    });

    const sheet = (page: HTMLElement) => page.querySelector('dialog[aria-labelledby="student-heading"]') as HTMLDialogElement;

    /** Names of the members that carry the "verantwortlich" tag. */
    const responsibleNames = (page: HTMLElement) =>
      [...page.querySelectorAll('.member')]
        .filter((member) => member.querySelector('.responsible-tag'))
        .map((member) => member.querySelector('.member-name')?.textContent?.trim());

    function open(page: HTMLElement, name: string) {
      (page.querySelector(`button[aria-label="${name} bearbeiten"]`) as HTMLButtonElement).click();
      fixture.detectChanges();
    }

    async function reloadWith(body: StudentGroup[]) {
      await settle();
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/student-groups' }).flush(body);
      await settle();
      fixture.detectChanges();
    }

    it('REQ-020: marks the responsible student on the plate', async () => {
      const page = await respond([group3(7)]);

      expect(responsibleNames(page)).toEqual(['Anna Berger']);
    });

    it('REQ-020: sends the request to mark a responsible student and shows the reloaded list', async () => {
      const page = await respond([group3(null)]);
      open(page, 'Anna Berger');

      (sheet(page).querySelector('.make-responsible') as HTMLButtonElement).click();
      await settle();
      const request = http.expectOne({ method: 'PUT', url: '/api/student-groups/3/responsible' });
      expect(request.request.body).toEqual({ studentId: 7 });
      request.flush({ id: 3, name: 'Group 3', responsibleStudentId: 7 });
      await reloadWith([group3(7)]);

      expect(responsibleNames(page)).toEqual(['Anna Berger']);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Anna Berger ist jetzt verantwortlich für Group 3.');
      expect(sheet(page).open).toBe(false);
    });

    it('REQ-020: changes the responsible student to another one', async () => {
      const page = await respond([group3(7)]);
      open(page, 'Ben Huber');

      (sheet(page).querySelector('.make-responsible') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'PUT', url: '/api/student-groups/3/responsible' }).flush({ id: 3, name: 'Group 3', responsibleStudentId: 8 });
      await reloadWith([group3(8)]);

      expect(responsibleNames(page)).toEqual(['Ben Huber']);
    });

    it('REQ-020: clears the responsible student', async () => {
      const page = await respond([group3(7)]);
      open(page, 'Anna Berger');

      (sheet(page).querySelector('.clear-responsible') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'DELETE', url: '/api/student-groups/3/responsible' }).flush({ id: 3, name: 'Group 3', responsibleStudentId: null });
      await reloadWith([group3(null)]);

      expect(responsibleNames(page)).toEqual([]);
      expect(page.querySelector('[role="status"]')?.textContent).toContain('Anna Berger ist nicht mehr verantwortlich für Group 3.');
    });

    it('REQ-020: shows the backend message when the student is not in the group (422)', async () => {
      const page = await respond([group3(null)]);
      open(page, 'Anna Berger');

      (sheet(page).querySelector('.make-responsible') as HTMLButtonElement).click();
      await settle();
      http.expectOne({ method: 'PUT', url: '/api/student-groups/3/responsible' }).flush(
        { status: 422, detail: '"Anna Berger" gehört nicht zur Gruppe "Group 3".' },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
      await settle();
      fixture.detectChanges();

      expect(sheet(page).querySelector('[role="alert"]')?.textContent).toContain('"Anna Berger" gehört nicht zur Gruppe "Group 3".');
      expect(sheet(page).open).toBe(true);
      http.expectNone({ method: 'GET', url: '/api/student-groups' });
    });
  });
});
