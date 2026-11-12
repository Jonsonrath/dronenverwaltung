export type LoanStatus = 'Active' | 'Returned';

/** A loan as returned by /api/loans (REQ-004 ... REQ-010). Dates are ISO strings in UTC. */
export interface Loan {
  id: number;
  droneId: number;
  droneName: string;
  studentGroupId: number;
  studentGroupName: string;
  batteries: number;
  propellers: number;
  loanDate: string;
  returnDate: string | null;
  status: LoanStatus;
  /**
   * Returned accessories (REQ-022). Null while the loan is active and for loans returned
   * before returned quantities were recorded – those count as complete.
   */
  returnedBatteries?: number | null;
  returnedPropellers?: number | null;
  /** Name of the group's responsible student at loan time (REQ-021); null if the group had none. */
  responsibleStudentName?: string | null;
  /** Damage reported on return (REQ-023); null when there is none. */
  damageReport?: string | null;
}

/**
 * Optional body of POST /api/loans/{id}/return (REQ-022). Missing numbers count as the loaned quantity.
 * A damage report (REQ-023) marks the drone as damaged; the backend treats empty text as no report.
 */
export interface ReturnLoanRequest {
  batteries?: number;
  propellers?: number;
  damageReport?: string;
}

/** Body of POST /api/loans (REQ-004, REQ-006). Missing quantities count as 0. */
export interface CreateLoanRequest {
  droneId: number;
  studentGroupId: number;
  batteries?: number;
  propellers?: number;
}
