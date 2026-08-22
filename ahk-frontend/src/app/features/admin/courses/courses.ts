import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  CourseDto,
  CourseHealthAdminClient,
  CoursesAdminClient,
  CreateCourseRequest,
  HealthStatus,
} from '../../../api/api-client';

/**
 * The site's course register. Each row carries the course's identity, its size, and a one-line verdict on its
 * integration, so an admin can tell a working course from a half-configured one without opening it. The full
 * chain of checks is one click away, on the health page and in the course editor.
 *
 * The verdict comes off the course row, cached: running the checks live costs seconds of GitHub round-trips
 * per course, and this page must paint at once. A verdict past its TTL is shown anyway — stale is far more
 * useful than blank — and a background refresh is queued behind it, landing on the next visit.
 */
@Component({
  selector: 'app-admin-courses',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './courses.html',
  styleUrl: './courses.scss',
})
export class AdminCourses implements OnInit {
  private readonly client = inject(CoursesAdminClient);
  private readonly healthClient = inject(CourseHealthAdminClient);
  private readonly router = inject(Router);

  protected readonly courses = signal<CourseDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly adding = signal(false);
  protected readonly saving = signal(false);
  protected slug = '';
  protected name = '';
  protected organization = '';

  /** Courses whose integration is failing — the number the page leads with. */
  protected readonly failingCount = computed(
    () => this.courses().filter((c) => c.healthStatus === 'Failed').length,
  );

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.loading.set(true);
    this.error.set(null);

    this.client.list().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        this.loading.set(false);
        this.queueRefresh(courses);
      },
      error: () => {
        this.error.set('Could not load the courses. Reload the page to try again.');
        this.loading.set(false);
      },
    });
  }

  /**
   * Asks the server to bring stale verdicts up to date in the background. Fire and forget: the request only
   * queues work, the table is already on screen, and a failed enqueue is not worth an error banner.
   */
  private queueRefresh(courses: CourseDto[]): void {
    if (!courses.some((c) => c.healthStale)) {
      return;
    }

    this.healthClient.refreshStale().subscribe({ error: () => undefined });
  }

  protected tone(status: HealthStatus | undefined): string {
    switch (status) {
      case 'Healthy':
        return 'ok';
      case 'Warning':
        return 'warn';
      case 'Failed':
        return 'bad';
      default:
        return '';
    }
  }

  protected verdict(status: HealthStatus | undefined): string {
    switch (status) {
      case 'Healthy':
        return 'Passing';
      case 'Warning':
        return 'Needs attention';
      case 'Failed':
        return 'Failing';
      default:
        return 'Not set up';
    }
  }

  protected startAdding(): void {
    this.adding.set(true);
    this.error.set(null);
  }

  protected cancelAdding(): void {
    this.adding.set(false);
    this.slug = '';
    this.name = '';
    this.organization = '';
  }

  /** Creates the course and opens its editor — the next thing to do is always fill in the integration. */
  protected create(): void {
    this.error.set(null);
    this.saving.set(true);

    const request: CreateCourseRequest = {
      slug: this.slug.trim(),
      name: this.name.trim(),
      gitHubOrganization: this.organization.trim() || undefined,
    };

    this.client.create(request).subscribe({
      next: (course) => {
        this.saving.set(false);
        this.cancelAdding();
        void this.router.navigate(['/admin/courses', course.id]);
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(
          (err as { status?: number }).status === 409
            ? `The slug "${request.slug}" is already taken. Pick another one.`
            : 'The course could not be created. Check the slug and name, then try again.',
        );
      },
    });
  }
}
