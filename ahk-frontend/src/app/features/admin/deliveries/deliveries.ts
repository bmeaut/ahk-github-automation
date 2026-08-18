import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

import {
  CoursesAdminClient,
  CourseDto,
  GitHubWebhookDeliveryStatus,
  WebhookDeliveriesAdminClient,
  WebhookDeliveryCountsDto,
  WebhookDeliveryDto,
  WebhookHandlerOutcome,
} from '../../../api/api-client';
import { readApiError } from '../../../core/api-error';

/**
 * The webhook delivery log.
 *
 * This exists because the receiver answers `202 Accepted` before any handler runs, which means GitHub's own
 * *Recent Deliveries* view can no longer say what happened — its response body is now written before the work
 * starts. What each handler made of a delivery is recorded against the row and shown here instead, along with
 * the one thing that view never offered: a re-run that skips the handlers which already worked.
 *
 * Refresh is a button and nothing else. A queued delivery becomes terminal within seconds, and a page that
 * reloads itself under an administrator who is reading an error message is worse than one they poke.
 */
@Component({
  selector: 'app-admin-deliveries',
  imports: [FormsModule, DatePipe],
  templateUrl: './deliveries.html',
  styleUrl: './deliveries.scss',
})
export class AdminDeliveries implements OnInit {
  private readonly client = inject(WebhookDeliveriesAdminClient);
  private readonly coursesClient = inject(CoursesAdminClient);

  protected readonly statuses: GitHubWebhookDeliveryStatus[] = [
    'Pending',
    'Processing',
    'Succeeded',
    'Failed',
    'Skipped',
    'Interrupted',
  ];

  protected readonly items = signal<WebhookDeliveryDto[]>([]);
  protected readonly counts = signal<WebhookDeliveryCountsDto>({});
  protected readonly total = signal(0);
  protected readonly courses = signal<CourseDto[]>([]);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly loadedAt = signal<Date | null>(null);

  protected courseId: number | null = null;
  protected status: GitHubWebhookDeliveryStatus | null = null;
  protected repository = '';

  /** The delivery whose outcomes are expanded, and what has been fetched for it. */
  protected readonly openId = signal<number | null>(null);
  protected readonly outcomes = signal<WebhookHandlerOutcome[]>([]);
  protected readonly detailError = signal<string | null>(null);
  protected readonly payload = signal<string | null>(null);
  protected readonly busy = signal(false);

  ngOnInit(): void {
    this.coursesClient.list().subscribe({
      next: (courses) => this.courses.set(courses),
      error: () => this.courses.set([]),
    });

    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.client.list(this.courseId, this.status, this.repository.trim() || null, 0, 50).subscribe({
      next: (page) => {
        this.items.set(page.items ?? []);
        this.counts.set(page.counts ?? {});
        this.total.set(page.total ?? 0);
        this.loadedAt.set(new Date());
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(readApiError(err, 'The delivery log could not be loaded.'));
        this.loading.set(false);
      },
    });
  }

  protected toggle(delivery: WebhookDeliveryDto): void {
    if (this.openId() === delivery.id) {
      this.openId.set(null);
      return;
    }

    this.openId.set(delivery.id ?? null);
    this.outcomes.set([]);
    this.payload.set(null);
    this.detailError.set(null);

    if (delivery.id === undefined) return;

    this.client.get(delivery.id).subscribe({
      next: (detail) => this.outcomes.set(detail.outcomes ?? []),
      error: (err) => this.detailError.set(readApiError(err, 'The handler outcomes could not be loaded.')),
    });
  }

  /** Fetched only when asked for: the payload is large, and it carries data out of private student repositories. */
  protected showPayload(id: number | undefined): void {
    if (id === undefined) return;

    this.client.getPayload(id).subscribe({
      next: (body) => this.payload.set(body),
      error: (err) => this.detailError.set(readApiError(err, 'The payload could not be loaded.')),
    });
  }

  protected retry(delivery: WebhookDeliveryDto, onlyFailedHandlers: boolean): void {
    if (delivery.id === undefined) return;

    this.busy.set(true);
    this.detailError.set(null);

    this.client.retry(delivery.id, { onlyFailedHandlers }).subscribe({
      next: () => {
        this.busy.set(false);
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.detailError.set(readApiError(err, 'The delivery could not be re-run.'));
      },
    });
  }

  protected tone(status: GitHubWebhookDeliveryStatus | undefined): string {
    switch (status) {
      case 'Succeeded':
        return 'ok';
      case 'Pending':
      case 'Processing':
      case 'Skipped':
        return 'warn';
      case 'Failed':
      case 'Interrupted':
        return 'bad';
      default:
        return '';
    }
  }

  protected duration(delivery: WebhookDeliveryDto): string {
    if (!delivery.receivedAt || !delivery.completedAt) return '—';

    const ms = new Date(delivery.completedAt).getTime() - new Date(delivery.receivedAt).getTime();
    return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} s`;
  }
}
