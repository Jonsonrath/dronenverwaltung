import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';

// Task 5.1: app shell, routes and navigation (design.md §10).
describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      // Pages load data; the testing backend keeps these navigation tests offline.
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  async function renderAt(url: string) {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl(url);
    fixture.detectChanges();
    // Answer the page's data requests with empty lists; only the shell is tested here.
    TestBed.inject(HttpTestingController).match(() => true).forEach((request) => request.flush([]));
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the five navigation links', async () => {
    const page = await renderAt('/drones');

    const labels = [...page.querySelectorAll('nav a .tab-label')].map((a) => a.textContent?.trim());
    expect(labels).toEqual(['Drohnen', 'Schülergruppen', 'Neue Ausleihe', 'Aktive Ausleihen', 'Verlauf']);
  });

  it('opens the drone list by default', async () => {
    const harness = await RouterTestingHarness.create('/');

    expect(TestBed.inject(Router).url).toBe('/drones');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Drohnen');
  });

  it.each([
    ['/drones', 'Drohnen'],
    ['/student-groups', 'Schülergruppen'],
    ['/loans/new', 'Neue Ausleihe'],
    ['/loans', 'Aktive Ausleihen'],
    ['/loans/history', 'Verlauf'],
  ])('shows the page for %s', async (url, heading) => {
    const harness = await RouterTestingHarness.create(url);

    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(heading);
  });

  it('marks only the current page as active', async () => {
    const page = await renderAt('/loans/history');

    const active = [...page.querySelectorAll('nav a[aria-current="page"] .tab-label')].map((a) => a.textContent?.trim());
    expect(active).toEqual(['Verlauf']);
  });

  it('sends unknown URLs to the drone list', async () => {
    await RouterTestingHarness.create('/does-not-exist');

    expect(TestBed.inject(Router).url).toBe('/drones');
  });
});
