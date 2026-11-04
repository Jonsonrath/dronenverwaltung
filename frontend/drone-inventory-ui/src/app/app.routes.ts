import { Routes } from '@angular/router';
import { Drones } from './features/drones/drones';
import { ActiveLoans } from './features/loans/active-loans';
import { LoanHistory } from './features/loans/loan-history';
import { NewLoan } from './features/loans/new-loan';
import { StudentGroups } from './features/student-groups/student-groups';

// Routes from design.md §10.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'drones' },
  { path: 'drones', component: Drones, title: 'Drones – Drone Lending' },
  { path: 'student-groups', component: StudentGroups, title: 'Student groups – Drone Lending' },
  { path: 'loans/new', component: NewLoan, title: 'New loan – Drone Lending' },
  { path: 'loans/history', component: LoanHistory, title: 'Loan history – Drone Lending' },
  { path: 'loans', component: ActiveLoans, title: 'Active loans – Drone Lending' },
  { path: '**', redirectTo: 'drones' },
];
