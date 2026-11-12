import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { AddStudentRequest, MoveStudentRequest, Student } from './student.model';

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

  /**
   * Moves a student to another group (REQ-018). The backend rejects an unknown student or group (404)
   * and a name the target group already has (409).
   */
  move(id: number, request: MoveStudentRequest): Observable<Student> {
    return this.http.put<Student>(`/api/students/${id}/group`, request);
  }

  /** Removes a student (REQ-019). The backend rejects an unknown student (404). */
  remove(id: number): Observable<void> {
    return this.http.delete<void>(`/api/students/${id}`);
  }
}
