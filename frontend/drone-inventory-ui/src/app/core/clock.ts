import { Service } from '@angular/core';

/** The current time, as a service so that tests can set it. */
@Service()
export class Clock {
  now(): Date {
    return new Date();
  }
}
