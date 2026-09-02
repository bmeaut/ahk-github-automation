import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { CourseContextService } from '../../core/course/course-context.service';

/**
 * Authenticated app frame: a topbar carrying identity and the course switcher, and a rail whose contents
 * depend on where you are — course screens inside a course, site screens under /admin. Admins can reach every
 * course from the switcher, so the same frame serves both jobs without a separate "admin mode".
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly courseContext = inject(CourseContextService);

  protected readonly user = this.auth.currentUser;
  protected readonly isAdmin = this.auth.isAdmin;
  protected readonly courses = this.auth.courses;

  protected readonly activeSlug = this.courseContext.activeSlug;
  protected readonly activeCourse = this.courseContext.activeCourse;

  /** True on the site-admin screens, which have no course context. */
  protected readonly inAdmin = computed(() => this.activeSlug() === null);

  /**
   * Id of the active course when the user administers it, for the "Manage course" link — null otherwise. Site
   * admins hold that role on every course (the API says so), so this needs no separate admin case.
   */
  protected readonly manageCourseId = computed(() => {
    const course = this.activeCourse();
    return course?.role === 'Admin' ? (course.id ?? null) : null;
  });

  protected switchCourse(slug: string): void {
    if (slug) {
      void this.router.navigate([slug, 'assignments']);
    }
  }

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
