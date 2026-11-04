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
      providers: [provideRouter(routes)],
    });
  });

  async function renderAt(url: string) {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl(url);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the five navigation links', async () => {
    const page = await renderAt('/drones');

    const labels = [...page.querySelectorAll('nav a')].map((a) => a.textContent?.trim());
    expect(labels).toEqual(['Drones', 'Student groups', 'New loan', 'Active loans', 'History']);
  });

  it('opens the drone list by default', async () => {
    const harness = await RouterTestingHarness.create('/');

    expect(TestBed.inject(Router).url).toBe('/drones');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Drones');
  });

  it.each([
    ['/drones', 'Drones'],
    ['/student-groups', 'Student groups'],
    ['/loans/new', 'New loan'],
    ['/loans', 'Active loans'],
    ['/loans/history', 'Loan history'],
  ])('shows the page for %s', async (url, heading) => {
    const harness = await RouterTestingHarness.create(url);

    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(heading);
  });

  it('marks only the current page as active', async () => {
    const page = await renderAt('/loans/history');

    const active = [...page.querySelectorAll('nav a[aria-current="page"]')].map((a) => a.textContent?.trim());
    expect(active).toEqual(['History']);
  });

  it('sends unknown URLs to the drone list', async () => {
    await RouterTestingHarness.create('/does-not-exist');

    expect(TestBed.inject(Router).url).toBe('/drones');
  });
});
