import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { MyAssignmentsClient, StudentRepository } from '../../api/api-client';
import { readApiError } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';

/** One course's worth of repositories, which is how the page is grouped. */
interface CourseGroup {
  slug: string;
  name: string;
  repositories: StudentRepository[];
}

/**
 * A student's own page: every repository they hold, across every course.
 *
 * This is also where a student lands when they sign in without an invite link, so it doubles as the
 * "you have nothing yet" screen — the empty state has to explain what to do, not just report a void.
 */
@Component({
  selector: 'app-my-assignments',
  imports: [DatePipe],
  templateUrl: './my-assignments.html',
  styleUrl: './my-assignments.scss',
})
export class MyAssignments implements OnInit {
  private readonly client = inject(MyAssignmentsClient);
  private readonly auth = inject(AuthService);

  protected readonly repositories = signal<StudentRepository[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly resending = signal<number | null>(null);
  protected readonly note = signal<string | null>(null);

  protected readonly user = this.auth.currentUser;

  /** Grouped by course, preserving the API's newest-first order within each. */
  protected readonly groups = computed<CourseGroup[]>(() => {
    const byCourse = new Map<string, CourseGroup>();

    for (const repository of this.repositories()) {
      const slug = repository.courseSlug ?? '';
      const group = byCourse.get(slug) ?? { slug, name: repository.courseName ?? slug, repositories: [] };
      group.repositories.push(repository);
      byCourse.set(slug, group);
    }

    return [...byCourse.values()];
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.client.list().subscribe({
      next: (repositories) => {
        this.repositories.set(repositories);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readApiError(err, 'Your assignments could not be loaded.'));
        this.loading.set(false);
      },
    });
  }

  /**
   * GitHub cannot extend an invitation, so this withdraws the stale one and issues a fresh one. The student
   * still has to click through it on GitHub — the button only puts a live invitation back in their inbox.
   */
  protected resend(repository: StudentRepository): void {
    const id = repository.acceptanceId ?? 0;

    this.error.set(null);
    this.note.set(null);
    this.resending.set(id);

    this.client.resendInvitation(id).subscribe({
      next: (updated) => {
        this.resending.set(null);
        this.repositories.update((list) => list.map((r) => (r.acceptanceId === id ? updated : r)));

        this.note.set(
          updated.access === 'Active'
            ? 'You already have access to that repository — no invitation was needed.'
            : 'A new invitation is on its way. Open it from the link next to the repository, or from your GitHub notifications.',
        );
      },
      error: (err: unknown) => {
        this.resending.set(null);
        this.error.set(readApiError(err, 'The invitation could not be sent. Try again in a few minutes.'));
      },
    });
  }

  protected logout(): void {
    this.auth.logout().subscribe();
  }
}
