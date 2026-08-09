import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { API_BASE_URL } from './api/api-client';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Empty base URL => generated clients issue relative /api/... requests, served same-origin through the
    // dev proxy (no CORS). In production the SPA is hosted under the same origin as the API.
    { provide: API_BASE_URL, useValue: '' },
  ],
};
