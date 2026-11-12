import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { StudentApi } from './student-api';

// Change student-group-members, task 2.1: StudentApi calls the backend (REQ-016).
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
});
