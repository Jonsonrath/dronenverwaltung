/** A student as listed with its group by GET /api/student-groups (REQ-017). Class is empty if not given. */
export interface Student {
  id: number;
  name: string;
  class: string;
  studentGroupId: number;
}

/** Body of POST /api/student-groups/{groupId}/students (REQ-016). */
export interface AddStudentRequest {
  name: string;
  class?: string;
}

/** Body of PUT /api/students/{id}/group (REQ-018). */
export interface MoveStudentRequest {
  studentGroupId: number;
}
