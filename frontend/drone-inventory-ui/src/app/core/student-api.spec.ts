import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { StudentApi } from './student-api';

// Change student-group-members, tasks 2.1 and 2.2: StudentApi calls the backend (REQ-016, REQ-018, REQ-019).
describe('StudentApi', () => {
  let api: StudentApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(StudentApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('REQ-016: add() sends POST /api/student-groups/{groupId}/students with name and class', async () => {
    const student = { id: 7, name: 'Anna Berger', class: '4AHIF', studentGroupId: 3 };

    const result = firstValueFrom(api.add(3, { name: 'Anna Berger', class: '4AHIF' }));
    const request = http.expectOne('/api/student-groups/3/students');
    request.flush(student, { status: 201, statusText: 'Created' });

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Anna Berger', class: '4AHIF' });
    expect(await result).toEqual(student);
  });

  it('REQ-018: move() sends PUT /api/students/{id}/group with the target group', async () => {
    const result = firstValueFrom(api.move(7, { studentGroupId: 1 }));
    const request = http.expectOne('/api/students/7/group');
    request.flush({ id: 7, name: 'Anna Berger', class: '4AHIF', studentGroupId: 1 });

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ studentGroupId: 1 });
    expect((await result).studentGroupId).toBe(1);
  });

  it('REQ-019: remove() sends DELETE /api/students/{id}', async () => {
    const result = firstValueFrom(api.remove(8));
    const request = http.expectOne('/api/students/8');
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(request.request.method).toBe('DELETE');
    await result;
  });
});
