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
  /**
   * `short` is the label in the bottom tab bar of phones, where five full labels do not fit.
   * `main` marks the main action; the bottom tab bar shows it larger, in the middle.
   */
  protected readonly links = [
    { path: '/drones', label: 'Drohnen', short: 'Drohnen', icon: 'drones', main: false },
    { path: '/student-groups', label: 'Schülergruppen', short: 'Gruppen', icon: 'groups', main: false },
    { path: '/loans/new', label: 'Neue Ausleihe', short: 'Ausleihen', icon: 'new', main: true },
    { path: '/loans', label: 'Aktive Ausleihen', short: 'Aktiv', icon: 'active', main: false },
    { path: '/loans/history', label: 'Verlauf', short: 'Verlauf', icon: 'history', main: false },
  ];
}
