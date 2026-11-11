import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** App shell: navigation (side, top or bottom, depending on the width) and the routed page. */
@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly links = [
    { path: '/drones', label: 'Drones', icon: 'drones' },
    { path: '/student-groups', label: 'Student groups', icon: 'groups' },
    { path: '/loans/new', label: 'New loan', icon: 'new' },
    { path: '/loans', label: 'Active loans', icon: 'active' },
    { path: '/loans/history', label: 'History', icon: 'history' },
  ];
}
