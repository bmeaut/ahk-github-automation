import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  CourseDto,
  CourseHealthAdminClient,
  CourseHealthReport,
  CoursesAdminClient,
  CreateCourseRequest,
  HealthStatus,
} from '../../../api/api-client';

/**
 * The site's course register. Each row carries the course's identity, its size, and a one-line verdict on its
 * integration, so an admin can tell a working course from a half-configured one without opening it. The full
 * chain of checks is one click away, on the health page and in the course editor.
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
  protected readonly health = signal<Map<number, CourseHealthReport>>(new Map());
  protected readonly loading = signal(false);
  protected readonly checking = signal(false);
  /** True once a health run has returned — until then the Integration column is honestly blank, not "passing". */
  protected readonly healthLoaded = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly adding = signal(false);
  protected readonly saving = signal(false);
  protected slug = '';
  protected name = '';
  protected organization = '';

  /** Courses whose integration is failing — the number the page leads with. */
  protected readonly failingCount = computed(
    () => [...this.health().values()].filter((r) => r.status === 'Failed').length,
  );

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.loading.set(true);
    this.healthLoaded.set(false);
    this.error.set(null);

    // The register itself is one cheap query, so it paints immediately. The health run is the slow half — it
    // checks every course in turn, and two of its checks call GitHub with a ten-second budget each — so it
    // follows as a second step and fills the Integration column in when it arrives.
    this.client.list().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        this.loading.set(false);
        this.recheck();
      },
      error: () => {
        this.error.set('Could not load the courses. Reload the page to try again.');
        this.loading.set(false);
      },
    });
  }

  protected recheck(): void {
    this.checking.set(true);
    this.healthClient.checkAll().subscribe({
      next: (reports) => {
        this.health.set(new Map(reports.map((r) => [r.courseId ?? 0, r])));
        this.healthLoaded.set(true);
        this.checking.set(false);
      },
      error: () => {
        // The table is already on screen; a failed health run only costs the Integration column.
        this.error.set('The health check could not be run.');
        this.checking.set(false);
      },
    });
  }

  protected reportFor(course: CourseDto): CourseHealthReport | undefined {
    return this.health().get(course.id ?? 0);
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

  /** Names the checks that are not passing, so the row says what to go and fix. */
  protected problems(report: CourseHealthReport): string {
    return (report.checks ?? [])
      .filter((c) => c.status !== 'Healthy')
      .map((c) => c.title)
      .join(', ');
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
