import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CourseHealthAdminClient, CourseHealthReport, HealthStatus } from '../../../api/api-client';
import { HealthChain } from '../../../shared/health-chain/health-chain';

/**
 * Health of every course in one place: which integrations work, which are half-configured, and what to do
 * about the ones that are not. Failing courses sort to the top, because that is the only reason to open this
 * page twice.
 */
@Component({
  selector: 'app-admin-health',
  imports: [RouterLink, DatePipe, HealthChain],
  templateUrl: './health.html',
  styleUrl: './health.scss',
})
export class AdminHealth implements OnInit {
  private readonly client = inject(CourseHealthAdminClient);

  protected readonly reports = signal<CourseHealthReport[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly checkedAt = signal<Date | null>(null);

  /** Worst first, then alphabetical — the reading order matches the order to act in. */
  private static readonly severity: Record<HealthStatus, number> = {
    Failed: 0,
    Warning: 1,
    NotConfigured: 2,
    Healthy: 3,
  };

  protected readonly sorted = computed(() =>
    [...this.reports()].sort((a, b) => {
      const bySeverity =
        AdminHealth.severity[a.status ?? 'NotConfigured'] - AdminHealth.severity[b.status ?? 'NotConfigured'];
      return bySeverity !== 0 ? bySeverity : (a.courseSlug ?? '').localeCompare(b.courseSlug ?? '');
    }),
  );

  protected readonly counts = computed(() => {
    const all = this.reports();
    return {
      total: all.length,
      failed: all.filter((r) => r.status === 'Failed').length,
      warning: all.filter((r) => r.status === 'Warning').length,
      notConfigured: all.filter((r) => r.status === 'NotConfigured').length,
      healthy: all.filter((r) => r.status === 'Healthy').length,
    };
  });

  ngOnInit(): void {
    this.run();
  }

  protected run(): void {
    this.loading.set(true);
    this.error.set(null);

    this.client.checkAll().subscribe({
      next: (reports) => {
        this.reports.set(reports);
        this.checkedAt.set(new Date());
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The checks could not be run. Reload the page to try again.');
        this.loading.set(false);
      },
    });
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

  protected summaryFor(report: CourseHealthReport): string {
    switch (report.status) {
      case 'Healthy':
        return 'Everything checks out.';
      case 'Failed':
        return 'Part of this integration is broken.';
      case 'Warning':
        return 'Works, but something needs attention.';
      default:
        return 'Not set up yet.';
    }
  }
}
