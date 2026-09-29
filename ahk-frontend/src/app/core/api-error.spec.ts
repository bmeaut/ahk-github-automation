import { SwaggerException } from '../api/api-client';
import { readApiError } from './api-error';

describe('readApiError', () => {
  const fallback = 'fallback';

  it('reads the message from a SwaggerException body', () => {
    const err = new SwaggerException('x', 502, '{"error":"GitHub could not be reached."}', {}, null);
    expect(readApiError(err, fallback)).toBe('GitHub could not be reached.');
  });

  it('reads the message from a typed error body thrown as-is', () => {
    // A declared 400 is typed ProblemDetails, so the generated client throws the parsed body, not a SwaggerException.
    const err = { error: 'The GitHub account "octocat" is already linked to another user here.' };
    expect(readApiError(err, fallback)).toBe('The GitHub account "octocat" is already linked to another user here.');
  });

  it('joins an errors array', () => {
    expect(readApiError({ errors: ['One.', 'Two.'] }, fallback)).toBe('One. Two.');
  });

  it('falls back to the title when errors is a validation dictionary', () => {
    const err = { title: 'One or more validation errors occurred.', errors: { name: ['Required'] } };
    expect(readApiError(err, fallback)).toBe('One or more validation errors occurred.');
  });

  it('reports an unreachable server', () => {
    const err = new SwaggerException('x', 0, '', {}, null);
    expect(readApiError(err, fallback)).toContain('not responding');
  });

  it('uses the fallback when there is no message', () => {
    expect(readApiError(new SwaggerException('x', 500, '', {}, null), fallback)).toBe(fallback);
    expect(readApiError(new Error('boom'), fallback)).toBe(fallback);
  });
});
