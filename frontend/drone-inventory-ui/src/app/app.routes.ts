import { Routes } from '@angular/router';
import { Drones } from './features/drones/drones';
import { ActiveLoans } from './features/loans/active-loans';
import { LoanHistory } from './features/loans/loan-history';
import { NewLoan } from './features/loans/new-loan';
import { StudentGroups } from './features/student-groups/student-groups';

// Routes from design.md §10.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'drones' },
  { path: 'drones', component: Drones, title: 'Drohnen – Drone Lending' },
  { path: 'student-groups', component: StudentGroups, title: 'Schülergruppen – Drone Lending' },
  { path: 'loans/new', component: NewLoan, title: 'Neue Ausleihe – Drone Lending' },
  { path: 'loans/history', component: LoanHistory, title: 'Verlauf – Drone Lending' },
  { path: 'loans', component: ActiveLoans, title: 'Aktive Ausleihen – Drone Lending' },
  { path: '**', redirectTo: 'drones' },
];
