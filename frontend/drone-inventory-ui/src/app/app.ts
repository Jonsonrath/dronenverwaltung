import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** App shell: top bar with the navigation and the routed page below. */
@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly links = [
    { path: '/drones', label: 'Drones' },
    { path: '/student-groups', label: 'Student groups' },
    { path: '/loans/new', label: 'New loan' },
    { path: '/loans', label: 'Active loans' },
    { path: '/loans/history', label: 'History' },
  ];
}
