import { Component, effect, inject, signal, untracked } from '@angular/core';
import { forkJoin } from 'rxjs';

import { GradesClient, SubmissionStatusesClient } from '../../api/api-client';
import { CourseContextService } from '../../core/course/course-context.service';

/** The four numbers, once both lists have arrived. */
interface Tiles {
  total: number;
  graded: number;
  openPrs: number;
  failing: number;
}

/**
 * Where a course stands at a glance: how many submissions there are, how many carry points, how many have a
 * pull request, and how many last failed their workflow. Course-wide always — it sits on the assignments
 * listing, which is the course's landing page, and no filter on any screen narrows it.
 *
 * It fetches its own two lists rather than taking them as inputs, so the page it sits on renders immediately
 * and the tiles appear when they are ready. Nothing is drawn until then: an empty row of zeroes would read as
 * a course with no submissions.
 */
@Component({
  selector: 'app-course-tally',
  templateUrl: './course-tally.html',
  styleUrl: './course-tally.scss',
})
export class CourseTally {
  private readonly statusesClient = inject(SubmissionStatusesClient);
  private readonly gradesClient = inject(GradesClient);
  private readonly courseContext = inject(CourseContextService);

  protected readonly tiles = signal<Tiles | null>(null);

  constructor() {
    // Same reason as the screens it sits on: the course switcher navigates between sibling routes, so this
    // component instance is reused and only the slug signal reports the change.
    effect(() => {
      const slug = this.courseContext.activeSlug();
      if (slug) {
        untracked(() => this.load(slug));
      }
    });
  }

  private load(slug: string): void {
    this.tiles.set(null);

    forkJoin({ statuses: this.statusesClient.list(slug), grades: this.gradesClient.list(slug) }).subscribe({
      next: ({ statuses, grades }) => {
        const graded = new Set(grades.map((g) => g.repo ?? ''));

        this.tiles.set({
          total: statuses.length,
          graded: statuses.filter((s) => graded.has(s.repository ?? '')).length,
          openPrs: statuses.filter((s) => (s.pullRequests?.length ?? 0) > 0).length,
          failing: statuses.filter((s) => s.workflowRuns?.lastStatus === 'failure').length,
        });
      },
      // Silent: these are a summary of a screen one click away, not the page's own content. Failing loudly
      // here would put an error banner above an assignments list that loaded perfectly well.
      error: () => this.tiles.set(null),
    });
  }
}
