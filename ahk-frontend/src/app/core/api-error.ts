/**
 * Turns a failed generated-client call into a sentence worth showing.
 *
 * The NSwag clients throw a `SwaggerException` carrying the raw response body, so the API's own message —
 * `{ "error": … }`, `{ "errors": [ … ] }` or a ProblemDetails `title` — has to be dug out. Status 0 is the
 * case that matters most in development: it means the request never reached the backend, and reporting that
 * as "wrong password" or "could not save" sends people looking in the wrong place.
 *
 * ⚠️ Not every failure is a `SwaggerException`. When an action declares a typed error response (a bare
 * `[ProducesResponseType(400)]` is typed as `ProblemDetails`), the generated `throwException` throws the
 * **parsed body itself** — so `{ "error": … }` arrives as the error object, with no `status` or `response`.
 */
export function readApiError(error: unknown, fallback: string): string {
  const status = (error as { status?: number }).status;

  if (status === 0) {
    return 'The server is not responding. Check that the backend is running, then try again.';
  }

  const body = (error as { response?: unknown }).response;
  if (typeof body === 'string' && body) {
    try {
      const message = messageOf(JSON.parse(body));
      if (message) {
        return message;
      }
    } catch {
      // Not JSON — fall through to the caller's wording.
    }
  } else {
    const message = messageOf(error);
    if (message) {
      return message;
    }
  }

  if (status === 403) {
    return 'You do not have access to do that.';
  }

  return fallback;
}

/**
 * The API's own sentence from an error body: `{ error }`, `{ errors: [...] }` (Identity results), or a
 * ProblemDetails `detail`/`title`. Only strings count — a thrown `HttpErrorResponse` also has an `error` field,
 * and model-validation ProblemDetails carry `errors` as a dictionary, not an array.
 */
function messageOf(value: unknown): string | null {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const body = value as { error?: unknown; errors?: unknown; detail?: unknown; title?: unknown };

  if (typeof body.error === 'string' && body.error) {
    return body.error;
  }

  if (Array.isArray(body.errors)) {
    const joined = body.errors.filter((e): e is string => typeof e === 'string').join(' ');
    if (joined) {
      return joined;
    }
  }

  for (const candidate of [body.detail, body.title]) {
    if (typeof candidate === 'string' && candidate) {
      return candidate;
    }
  }

  return null;
}
