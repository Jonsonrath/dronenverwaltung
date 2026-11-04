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
}

/** Body of POST /api/loans (REQ-004, REQ-006). Missing quantities count as 0. */
export interface CreateLoanRequest {
  droneId: number;
  studentGroupId: number;
  batteries?: number;
  propellers?: number;
}
