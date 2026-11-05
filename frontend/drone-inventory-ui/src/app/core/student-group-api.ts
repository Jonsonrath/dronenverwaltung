import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateStudentGroupRequest, StudentGroup } from './student-group.model';

/** Backend calls for student groups (design.md §6). */
@Service()
export class StudentGroupApi {
  private readonly http = inject(HttpClient);

  /** All student groups, ordered by name (REQ-003). */
  list(): Observable<StudentGroup[]> {
    return this.http.get<StudentGroup[]>('/api/student-groups');
  }

  /** Creates a student group (REQ-003). The backend rejects a blank name (422) or a duplicate (409). */
  create(request: CreateStudentGroupRequest): Observable<StudentGroup> {
    return this.http.post<StudentGroup>('/api/student-groups', request);
  }
}
