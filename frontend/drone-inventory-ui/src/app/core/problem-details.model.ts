/** Error body of the backend (RFC 7807), e.g. for 404 / 409 / 422 (design.md §5). */
export interface ProblemDetails {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}
