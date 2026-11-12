import { Component, input, model } from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';

/**
 * A number with − and + buttons, for batteries and propellers (design §4).
 * Works with Signal Forms: `<app-quantity-stepper [formField]="form.batteries" />`.
 *
 * "−" stops at 0 only as a convenience; a negative number can still be typed and the
 * backend answers with 422 – the rule "quantities are never negative" stays in the backend.
 */
@Component({
  selector: 'app-quantity-stepper',
  templateUrl: './quantity-stepper.html',
  styleUrl: './quantity-stepper.css',
})
export class QuantityStepper implements FormValueControl<number> {
  /** The bound quantity (Signal Forms keeps it in sync with the field). */
  readonly value = model(0);

  /** Set by Signal Forms while the form is submitting or the field is disabled. */
  readonly disabled = input(false);

  /** What is counted, for the button labels: "Akkus" → "Weniger Akkus" / "Mehr Akkus". */
  readonly label = input.required<string>();

  /** Id of the number field, so a `<label for>` outside can point to it. */
  readonly inputId = input<string>();

  protected decrement() {
    this.value.update((current) => current - 1);
  }

  protected increment() {
    this.value.update((current) => current + 1);
  }

  protected typed(event: Event) {
    const typed = (event.target as HTMLInputElement).valueAsNumber;
    // An empty field counts as 0, like a missing quantity in the API.
    this.value.set(Number.isNaN(typed) ? 0 : typed);
  }
}
