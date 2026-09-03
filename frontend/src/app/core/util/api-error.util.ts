import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns an HTTP failure into a sentence a user can act on. The API answers with
 * ProblemDetails, so its title is preferred when one is present.
 */
export function describeApiError(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'The API is not reachable. Start the backend and try again.';
  }

  const problem = error.error as
    | { title?: string; detail?: string; errors?: Record<string, string[]> }
    | null;

  // A 400 carries the useful text in `errors`, not in `detail`.
  const firstFieldError = problem?.errors
    ? Object.values(problem.errors).flat().find(message => message.length > 0)
    : undefined;

  return problem?.detail ?? firstFieldError ?? problem?.title ?? fallback;
}

/** Field errors from a 400 ValidationProblemDetails response, keyed by property. */
export function extractValidationErrors(error: unknown): Record<string, string[]> | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400) {
    return null;
  }

  const problem = error.error as { errors?: Record<string, string[]> } | null;

  return problem?.errors ?? null;
}
