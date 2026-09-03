import { DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';

import {
  AssignmentDto,
  AssignmentsClient,
  FinalStudentGrade,
  GradesClient,
  RepositoryStatus,
  SubmissionStatusesClient,
  SubmissionsClient,
} from '../../../api/api-client';
import { readApiError } from '../../../core/api-error';
import { CourseContextService } from '../../../core/course/course-context.service';

type SortKey = 'neptun' | 'repository' | 'runs' | 'total';

/** All submissions, one assignment's, or the ones no assignment claims. */
type AssignmentScope = 'all' | 'none' | number;

/**
 * The instructor's view of a course: every submission, its state on GitHub, and its points. Site admins reach
 * the same screen for any course, so nothing here assumes an explicit membership.
 *
 * Search, filtering and sorting all happen client-side — a course is a few hundred rows at most, and a
 * round-trip per keystroke would be slower than the reader.
 *
 * The assignment scope is the exception: it lives in the <code>assignment</code> query parameter, because the
 * assignments listing links here with one already chosen.
 */
@Component({
  selector: 'app-course-submissions',
  imports: [DecimalPipe, FormsModule],
  templateUrl: './submissions.html',
  styleUrl: './submissions.scss',
})
export class CourseSubmissions {
  private readonly statusesClient = inject(SubmissionStatusesClient);
  private readonly gradesClient = inject(GradesClient);
  private readonly assignmentsClient = inject(AssignmentsClient);
  private readonly submissionsClient = inject(SubmissionsClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly courseContext = inject(CourseContextService);

  protected readonly statuses = signal<RepositoryStatus[]>([]);
  protected readonly grades = signal<Map<string, FinalStudentGrade>>(new Map());
  protected readonly assignments = signal<AssignmentDto[]>([]);
  protected readonly exerciseNames = signal<string[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly saved = signal<string | null>(null);

  /**
   * Archiving is the course admins' to do, and a site admin arrives holding that role on every course — so
   * this is the same derivation the shell's "Manage course" item and courseManageGuard use.
   */
  protected readonly canArchive = computed(() => this.courseContext.activeCourse()?.role === 'Admin');

  protected search = '';
  protected readonly searchTerm = signal('');
  protected readonly onlyUngraded = signal(false);
  protected readonly onlyFailing = signal(false);
  protected readonly sortKey = signal<SortKey>('neptun');
  protected readonly sortAsc = signal(true);
  protected readonly assignmentScope = signal<AssignmentScope>('all');

  /**
   * The one filter the server applies: archived rows are left out of the response unless this is on, so
   * flipping it reloads. Everything else here narrows a list already in memory.
   */
  protected readonly showArchived = signal(false);

  protected readonly rows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const grades = this.grades();

    const scope = this.assignmentScope();

    let rows = this.statuses().filter((s) => {
      if (scope === 'none' && s.assignmentId != null) {
        return false;
      }
      if (typeof scope === 'number' && s.assignmentId !== scope) {
        return false;
      }
      if (term && !`${s.neptun ?? ''} ${s.repository ?? ''}`.toLowerCase().includes(term)) {
        return false;
      }
      if (this.onlyUngraded() && grades.has(s.repository ?? '')) {
        return false;
      }
      if (this.onlyFailing() && s.workflowRuns?.lastStatus !== 'failure') {
        return false;
      }
      return true;
    });

    const key = this.sortKey();
    const direction = this.sortAsc() ? 1 : -1;
    rows = [...rows].sort((a, b) => direction * this.compare(a, b, key));
    return rows;
  });

  constructor() {
    // The course switcher navigates between sibling /{course}/submissions routes, so the router reuses this
    // component instance and ngOnInit would fire only for the first course. Loading off the slug signal keeps
    // the tallies and the table with the header, which reads that same signal. Filters are course-specific —
    // a Neptun search carried over from the previous course would show an empty table for the new one.
    effect(() => {
      const slug = this.courseContext.activeSlug();
      if (slug) {
        untracked(() => {
          this.clearFilters();

          // Not part of clearFilters: arriving from the assignments listing carries a scope in the URL, and
          // resetting it here would drop the filter on the first paint. Switching course drops the parameter
          // with the navigation, so the scope resets by itself.
          this.assignmentScope.set(this.scopeFromUrl());
          this.showArchived.set(false);
          this.load(slug);
        });
      }
    });
  }

  /** Archived rows come from the server or not at all, so this reloads rather than re-filtering. */
  protected toggleArchived(): void {
    this.showArchived.update((show) => !show);
    this.reload();
  }

  /** Refresh: reloads whichever course is currently in context. */
  protected reload(): void {
    const slug = this.courseContext.activeSlug();
    if (slug) {
      this.load(slug);
    }
  }

  private load(slug: string): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      statuses: this.statusesClient.list(slug, this.showArchived()),
      // The same flag: a row shown because "Show archived" is on must still have its points beside it.
      grades: this.gradesClient.list(slug, this.showArchived()),
      // Archived included: a link to an archived assignment must still name it in the dropdown.
      assignments: this.assignmentsClient.list(slug, true),
    }).subscribe({
      next: ({ statuses, grades, assignments }) => {
        this.statuses.set(statuses);
        this.grades.set(new Map(grades.map((g) => [g.repo ?? '', g])));
        this.assignments.set(assignments);

        // Union of exercise names across students drives the table columns, like the CSV export.
        const names = new Set<string>();
        for (const g of grades) {
          Object.keys(g.points ?? {}).forEach((n) => names.add(n));
        }
        this.exerciseNames.set([...names].sort());
        this.loading.set(false);
      },
      error: () => {
        this.error.set('This course’s submissions could not be loaded.');
        this.loading.set(false);
      },
    });
  }

  protected setArchived(status: RepositoryStatus, archived: boolean): void {
    this.error.set(null);
    this.saved.set(null);

    const slug = this.courseContext.activeSlug();
    const id = status.submissionId ?? 0;
    if (!slug || !id) {
      return;
    }

    const request = archived
      ? this.submissionsClient.archive(id, slug)
      : this.submissionsClient.unarchive(id, slug);

    request.subscribe({
      next: () => {
        this.saved.set(
          archived
            ? `${status.repository} was archived. It stays out of the lists until it is reactivated.`
            : `${status.repository} is active again.`,
        );
        // Reload rather than patch the row: archiving one usually removes it from the current view.
        this.reload();
      },
      error: (err: unknown) =>
        this.error.set(readApiError(err, 'That submission could not be changed.')),
    });
  }

  protected onSearchChange(): void {
    this.searchTerm.set(this.search);
  }

  /** Applies a scope and mirrors it into the URL, so the filtered view can be linked to and reloaded. */
  protected setAssignmentScope(scope: AssignmentScope): void {
    this.assignmentScope.set(scope);

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { assignment: scope === 'all' ? null : scope },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  /** The scope named by the current URL: an assignment id, "none", or everything. */
  private scopeFromUrl(): AssignmentScope {
    const value = this.route.snapshot.queryParamMap.get('assignment');
    if (value === 'none') {
      return 'none';
    }
    const id = Number(value);
    return value !== null && Number.isFinite(id) && id > 0 ? id : 'all';
  }

  protected sortBy(key: SortKey): void {
    if (this.sortKey() === key) {
      this.sortAsc.set(!this.sortAsc());
    } else {
      this.sortKey.set(key);
      this.sortAsc.set(true);
    }
  }

  protected pointsFor(repo: string | undefined, exercise: string): number | null {
    return this.grades().get(repo ?? '')?.points?.[exercise] ?? null;
  }

  protected totalFor(repo: string | undefined): number | null {
    const points = this.grades().get(repo ?? '')?.points;
    if (!points) {
      return null;
    }
    return Object.values(points).reduce((sum, p) => sum + p, 0);
  }

  protected prUrlFor(status: RepositoryStatus): string | undefined {
    return status.pullRequests?.[0]?.htmlUrl ?? undefined;
  }

  protected downloadCsv(): void {
    const slug = this.courseContext.activeSlug();
    if (slug) {
      window.location.href = `/api/${slug}/grades/csv`;
    }
  }

  /**
   * Clears everything the "Clear" buttons offer to clear, the assignment scope included — a scope that is
   * hiding every row is exactly what someone pressing Clear wants gone. Only the buttons use this: the
   * course-change path must not navigate, so it calls {@link clearFilters}.
   */
  protected clearAllFilters(): void {
    this.clearFilters();
    this.setAssignmentScope('all');

    if (this.showArchived()) {
      this.toggleArchived();
    }
  }

  /** Clears the refinements over the current scope, without touching the scope or the URL. */
  private clearFilters(): void {
    this.search = '';
    this.searchTerm.set('');
    this.onlyUngraded.set(false);
    this.onlyFailing.set(false);
  }

  protected readonly filtered = computed(
    () =>
      this.searchTerm().trim().length > 0 ||
      this.onlyUngraded() ||
      this.onlyFailing() ||
      this.assignmentScope() !== 'all' ||
      this.showArchived(),
  );

  private compare(a: RepositoryStatus, b: RepositoryStatus, key: SortKey): number {
    switch (key) {
      case 'repository':
        return (a.repository ?? '').localeCompare(b.repository ?? '');
      case 'runs':
        return (a.workflowRuns?.count ?? 0) - (b.workflowRuns?.count ?? 0);
      case 'total':
        return (this.totalFor(a.repository) ?? -1) - (this.totalFor(b.repository) ?? -1);
      default:
        return (a.neptun ?? '').localeCompare(b.neptun ?? '');
    }
  }
}
