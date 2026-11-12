import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateStudentGroupRequest, RenameStudentGroupRequest, StudentGroup } from './student-group.model';

/** Backend calls for student groups (design.md §6; rename and delete: change manage-student-groups). */
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

  /**
   * Renames a student group (REQ-011). The backend rejects an unknown group (404),
   * a blank name (422) and the name of another group (409).
   */
  rename(id: number, request: RenameStudentGroupRequest): Observable<StudentGroup> {
    return this.http.put<StudentGroup>(`/api/student-groups/${id}`, request);
  }

  /**
   * Deletes a student group (REQ-012). The backend rejects an unknown group (404)
   * and a group that has a loan, active or returned (409).
   */
  remove(id: number): Observable<void> {
    return this.http.delete<void>(`/api/student-groups/${id}`);
  }
}
