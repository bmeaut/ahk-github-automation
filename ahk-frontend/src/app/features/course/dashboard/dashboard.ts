import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { FinalStudentGrade, GradesClient, RepositoryStatus, SubmissionStatusesClient } from '../../../api/api-client';
import { CourseContextService } from '../../../core/course/course-context.service';

type SortKey = 'neptun' | 'repository' | 'runs' | 'total';

/**
 * The instructor's view of a course: every submission, its state on GitHub, and its points. Site admins reach
 * the same screen for any course, so nothing here assumes an explicit membership.
 *
 * Search, filtering and sorting all happen client-side — a course is a few hundred rows at most, and a
 * round-trip per keystroke would be slower than the reader.
 */
@Component({
  selector: 'app-course-dashboard',
  imports: [DecimalPipe, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class CourseDashboard implements OnInit {
  private readonly statusesClient = inject(SubmissionStatusesClient);
  private readonly gradesClient = inject(GradesClient);
  protected readonly courseContext = inject(CourseContextService);

  protected readonly statuses = signal<RepositoryStatus[]>([]);
  protected readonly grades = signal<Map<string, FinalStudentGrade>>(new Map());
  protected readonly exerciseNames = signal<string[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected search = '';
  protected readonly searchTerm = signal('');
  protected readonly onlyUngraded = signal(false);
  protected readonly onlyFailing = signal(false);
  protected readonly sortKey = signal<SortKey>('neptun');
  protected readonly sortAsc = signal(true);

  protected readonly rows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const grades = this.grades();

    let rows = this.statuses().filter((s) => {
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

  protected readonly summary = computed(() => {
    const all = this.statuses();
    const grades = this.grades();
    return {
      total: all.length,
      graded: all.filter((s) => grades.has(s.repository ?? '')).length,
      openPrs: all.filter((s) => (s.pullRequests?.length ?? 0) > 0).length,
      failing: all.filter((s) => s.workflowRuns?.lastStatus === 'failure').length,
    };
  });

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    const slug = this.courseContext.activeSlug();
    if (!slug) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    forkJoin({ statuses: this.statusesClient.list(slug), grades: this.gradesClient.list(slug) }).subscribe({
      next: ({ statuses, grades }) => {
        this.statuses.set(statuses);
        this.grades.set(new Map(grades.map((g) => [g.repo ?? '', g])));

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

  protected onSearchChange(): void {
    this.searchTerm.set(this.search);
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

  protected clearFilters(): void {
    this.search = '';
    this.searchTerm.set('');
    this.onlyUngraded.set(false);
    this.onlyFailing.set(false);
  }

  protected readonly filtered = computed(
    () => this.searchTerm().trim().length > 0 || this.onlyUngraded() || this.onlyFailing(),
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
