import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { StudentGroupApi } from './student-group-api';
import { StudentGroup } from './student-group.model';

// Task 6.3: StudentGroupApi calls the backend (REQ-003).
describe('StudentGroupApi', () => {
  let api: StudentGroupApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(StudentGroupApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('REQ-003: list() sends GET /api/student-groups and returns the groups', async () => {
    const groups: StudentGroup[] = [
      { id: 1, name: 'Group 1' },
      { id: 3, name: 'Group 3' },
    ];

    const result = firstValueFrom(api.list());
    const request = http.expectOne('/api/student-groups');
    request.flush(groups);

    expect(request.request.method).toBe('GET');
    expect(await result).toEqual(groups);
  });

  it('REQ-003: create() sends POST /api/student-groups with the name', async () => {
    const result = firstValueFrom(api.create({ name: 'Group 3' }));
    const request = http.expectOne('/api/student-groups');
    request.flush({ id: 3, name: 'Group 3' });

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Group 3' });
    expect(await result).toEqual({ id: 3, name: 'Group 3' });
  });
});
