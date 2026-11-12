import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './problem-details.model';

/**
 * Text to show for a failed backend call. The backend decides what is wrong
 * (404 / 409 / 422 as ProblemDetails, design.md §5); the frontend only displays it.
 */
export function problemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'Das Backend ist nicht erreichbar. Läuft es? Dann erneut versuchen.';
    }
    const problem = error.error as Partial<ProblemDetails> | null;
    if (problem?.detail) {
      return problem.detail;
    }
    const firstFieldError = problem?.errors && Object.values(problem.errors)[0]?.[0];
    if (firstFieldError) {
      return firstFieldError;
    }
    if (problem?.title) {
      return problem.title;
    }
  }
  return 'Das hat nicht geklappt. Erneut versuchen.';
}
