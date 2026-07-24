/**
 * Turns a failed generated-client call into a sentence worth showing.
 *
 * The NSwag clients throw a `SwaggerException` carrying the raw response body, so the API's own message —
 * `{ "error": … }`, `{ "errors": [ … ] }` or a ProblemDetails `title` — has to be dug out. Status 0 is the
 * case that matters most in development: it means the request never reached the backend, and reporting that
 * as "wrong password" or "could not save" sends people looking in the wrong place.
 */
export function readApiError(error: unknown, fallback: string): string {
  const status = (error as { status?: number }).status;

  if (status === 0) {
    return 'The server is not responding. Check that the backend is running, then try again.';
  }

  const body = (error as { response?: string }).response;
  if (body) {
    try {
      const parsed = JSON.parse(body) as { error?: string; errors?: string[]; title?: string; detail?: string };
      const message = parsed.error ?? parsed.errors?.join(' ') ?? parsed.detail ?? parsed.title;
      if (message) {
        return message;
      }
    } catch {
      // Not JSON — fall through to the caller's wording.
    }
  }

  if (status === 403) {
    return 'You do not have access to do that.';
  }

  return fallback;
}
