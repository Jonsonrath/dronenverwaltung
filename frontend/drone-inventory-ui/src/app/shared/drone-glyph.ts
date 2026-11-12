import { Component, input } from '@angular/core';

/**
 * A quadcopter seen from above – the picture of a drone in its cabinet bay (design §7).
 * Decorative only: the status is always written next to it as a word.
 */
@Component({
  selector: 'app-drone-glyph',
  templateUrl: './drone-glyph.html',
  styleUrl: './drone-glyph.css',
  host: { '[class.is-out]': 'out()' },
})
export class DroneGlyph {
  /** Drawn as a dashed outline: the drone is not in its bay. */
  readonly out = input(false);
}
