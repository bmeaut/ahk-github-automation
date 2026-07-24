import { Injectable, computed, inject, signal } from '@angular/core';

import { AuthService } from '../auth/auth.service';

/**
 * Tracks the course the current route is scoped to (the {course} path segment), or null on the site-admin
 * screens. The active course record is derived from the courses {@link AuthService} says the user can open.
 */
@Injectable({ providedIn: 'root' })
export class CourseContextService {
  private readonly auth = inject(AuthService);

  private readonly slug = signal<string | null>(null);

  readonly activeSlug = this.slug.asReadonly();
  readonly activeCourse = computed(() => {
    const s = this.slug();
    return s ? (this.auth.courses().find((c) => c.slug === s) ?? null) : null;
  });

  setActiveSlug(slug: string | null): void {
    this.slug.set(slug);
  }
}
