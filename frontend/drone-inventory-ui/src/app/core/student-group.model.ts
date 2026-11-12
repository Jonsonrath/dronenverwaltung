import { Student } from './student.model';

/** A student group as returned by /api/student-groups. */
export interface StudentGroup {
  id: number;
  name: string;
  /** Members ordered by name (REQ-017); only in the list from GET /api/student-groups. */
  students?: Student[];
}

/** Body of POST /api/student-groups (REQ-003). */
export interface CreateStudentGroupRequest {
  name: string;
}

/** Body of PUT /api/student-groups/{id} (REQ-011). */
export interface RenameStudentGroupRequest {
  name: string;
}
