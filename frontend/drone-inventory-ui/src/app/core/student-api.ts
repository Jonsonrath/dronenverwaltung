import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { AddStudentRequest, Student } from './student.model';

/** Backend calls for the students of a group (change student-group-members, design §2). */
@Service()
export class StudentApi {
  private readonly http = inject(HttpClient);

  /**
   * Adds a student to a group (REQ-016). The backend rejects an unknown group (404),
   * a blank name (422) and a name the group already has (409).
   */
  add(studentGroupId: number, request: AddStudentRequest): Observable<Student> {
    return this.http.post<Student>(`/api/student-groups/${studentGroupId}/students`, request);
  }
}
