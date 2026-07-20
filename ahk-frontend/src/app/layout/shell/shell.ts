import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { CourseContextService } from '../../core/course/course-context.service';

/** Authenticated app frame: header with the active course, a course switcher, admin link and logout. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink],
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

  protected switch(slug: string): void {
    void this.router.navigate([slug, 'dashboard']);
  }

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
