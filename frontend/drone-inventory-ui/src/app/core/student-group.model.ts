/** A student group as returned by GET /api/student-groups. */
export interface StudentGroup {
  id: number;
  name: string;
}

/** Body of POST /api/student-groups (REQ-003). */
export interface CreateStudentGroupRequest {
  name: string;
}
