import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, FormField } from '@angular/forms/signals';
import { QuantityStepper } from './quantity-stepper';

// Task 3.1 (change improve-frontend-design): − / number / + control for batteries and propellers.

@Component({
  imports: [QuantityStepper, FormField],
  template: `<app-quantity-stepper label="batteries" inputId="qty" [formField]="accessories.batteries" />`,
})
class Host {
  readonly model = signal({ batteries: 0 });
  readonly accessories = form(this.model);
}

describe('QuantityStepper', () => {
  let fixture: ComponentFixture<Host>;
  let host: Host;
  let element: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({ imports: [Host] });
    fixture = TestBed.createComponent(Host);
    host = fixture.componentInstance;
    element = fixture.nativeElement;
    await fixture.whenStable();
    fixture.detectChanges();
  });

  const more = () => element.querySelector('button[aria-label="More batteries"]') as HTMLButtonElement;
  const fewer = () => element.querySelector('button[aria-label="Fewer batteries"]') as HTMLButtonElement;
  const field = () => element.querySelector('input') as HTMLInputElement;

  async function click(button: HTMLButtonElement) {
    button.click();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('+ increases the bound form field', async () => {
    await click(more());
    await click(more());

    expect(host.model().batteries).toBe(2);
    expect(field().value).toBe('2');
  });

  it('− decreases the bound form field', async () => {
    host.model.set({ batteries: 4 });
    fixture.detectChanges();

    await click(fewer());

    expect(host.model().batteries).toBe(3);
  });

  it('− is disabled at 0', () => {
    expect(host.model().batteries).toBe(0);
    expect(fewer().disabled).toBe(true);
    expect(more().disabled).toBe(false);
  });

  it('a typed value goes into the form field, also a negative one (the backend decides)', async () => {
    field().value = '8';
    field().dispatchEvent(new Event('input'));
    expect(host.model().batteries).toBe(8);

    field().value = '-1';
    field().dispatchEvent(new Event('input'));
    expect(host.model().batteries).toBe(-1);
  });

  it('an emptied field counts as 0', () => {
    field().value = '';
    field().dispatchEvent(new Event('input'));

    expect(host.model().batteries).toBe(0);
  });

  it('shows a value set from the form model', () => {
    host.model.set({ batteries: 6 });
    fixture.detectChanges();

    expect(field().value).toBe('6');
  });

  it('gives the number field the id for an outside label', () => {
    expect(field().id).toBe('qty');
  });
});
