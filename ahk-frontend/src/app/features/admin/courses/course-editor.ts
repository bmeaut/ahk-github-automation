import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  CourseDetailDto,
  CourseHealthAdminClient,
  CourseHealthReport,
  CourseMemberDto,
  CourseRole,
  CoursesAdminClient,
  UpdateCourseGitHubConfigRequest,
  UpdateCourseRequest,
  UserDto,
  UsersAdminClient,
  WebhookTokenDto,
} from '../../../api/api-client';
import { HealthChain } from '../../../shared/health-chain/health-chain';

/**
 * Everything one course holds, on one page: what it is, how it talks to GitHub, which tokens accept its
 * evaluation results, and who staffs it.
 *
 * Stored credentials are never sent to the browser, so their inputs start empty and mean "leave as is". Only a
 * field the admin actually typed into is submitted — that is what lets an unchanged form be saved safely.
 */
@Component({
  selector: 'app-course-editor',
  imports: [FormsModule, RouterLink, DatePipe, HealthChain],
  templateUrl: './course-editor.html',
  styleUrl: './course-editor.scss',
})
export class CourseEditor implements OnInit {
  private readonly client = inject(CoursesAdminClient);
  private readonly usersClient = inject(UsersAdminClient);
  private readonly healthClient = inject(CourseHealthAdminClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly course = signal<CourseDetailDto | null>(null);
  protected readonly report = signal<CourseHealthReport | null>(null);
  protected readonly loading = signal(true);
  protected readonly checking = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly saved = signal<string | null>(null);

  private courseId = 0;

  // ---- Settings form ----
  protected slug = '';
  protected name = '';
  protected organization = '';
  protected repoPrefix = '';
  protected readonly savingSettings = signal(false);

  /** Renaming the slug moves every URL the course is reachable at, so the form says so before it is saved. */
  protected readonly slugChanged = computed(() => this.course() !== null && this.slug !== this.course()?.slug);

  // ---- GitHub integration form ----
  protected appId = '';
  protected appPrivateKey = '';
  protected accessToken = '';
  protected webhookSecret = '';
  protected workflowRunThreshold = 5;
  protected integrationEnabled = true;
  protected readonly savingIntegration = signal(false);
  protected readonly clearing = signal<Record<string, boolean>>({});

  // ---- Tokens ----
  protected newTokenDescription = '';
  protected readonly issuedToken = signal<WebhookTokenDto | null>(null);
  protected readonly creatingToken = signal(false);
  /** Token id most recently copied, for transient "Copied" feedback. */
  protected readonly copiedTokenId = signal<number | null>(null);
  /** Token ids whose secret is currently revealed inline. */
  protected readonly revealed = signal<Set<number>>(new Set());

  // ---- Staff ----
  protected memberSearch = '';
  protected readonly candidates = signal<UserDto[]>([]);
  protected readonly searching = signal(false);

  // ---- Delete ----
  protected confirmSlug = '';
  protected readonly deleting = signal(false);

  ngOnInit(): void {
    this.courseId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
    this.runHealthCheck();
  }

  private load(): void {
    this.loading.set(true);
    this.client.get(this.courseId).subscribe({
      next: (course) => {
        this.course.set(course);
        this.slug = course.slug ?? '';
        this.name = course.name ?? '';
        this.organization = course.gitHubOrganization ?? '';
        this.repoPrefix = course.repoNamePrefix ?? '';
        this.appId = course.gitHubConfig?.gitHubAppId ?? '';
        this.workflowRunThreshold = course.gitHubConfig?.workflowRunThreshold ?? 5;
        this.integrationEnabled = course.gitHubConfig?.enabled ?? true;
        this.loading.set(false);
      },
      error: () => {
        this.error.set('That course could not be loaded.');
        this.loading.set(false);
      },
    });
  }

  protected runHealthCheck(): void {
    this.checking.set(true);
    this.healthClient.checkCourse(this.courseId).subscribe({
      next: (report) => {
        this.report.set(report);
        this.checking.set(false);
      },
      error: () => this.checking.set(false),
    });
  }

  // ---- Settings ----

  protected saveSettings(): void {
    this.clearMessages();
    this.savingSettings.set(true);

    const request: UpdateCourseRequest = {
      slug: this.slug.trim(),
      name: this.name.trim(),
      gitHubOrganization: this.organization.trim() || undefined,
      repoNamePrefix: this.repoPrefix.trim() || undefined,
    };

    this.client.update(this.courseId, request).subscribe({
      next: () => {
        this.savingSettings.set(false);
        this.saved.set('Course settings saved.');
        this.load();
        this.runHealthCheck();
      },
      error: (err: unknown) => {
        this.savingSettings.set(false);
        this.error.set(
          (err as { status?: number }).status === 409
            ? `The slug "${request.slug}" belongs to another course. Pick a different one.`
            : 'The settings could not be saved.',
        );
      },
    });
  }

  // ---- GitHub integration ----

  /** Marks a stored credential for removal; the input is then disabled until the change is saved or undone. */
  protected toggleClear(field: string): void {
    const next = { ...this.clearing() };
    next[field] = !next[field];
    this.clearing.set(next);
  }

  protected isClearing(field: string): boolean {
    return this.clearing()[field] === true;
  }

  protected saveIntegration(): void {
    this.clearMessages();
    this.savingIntegration.set(true);

    const request: UpdateCourseGitHubConfigRequest = {
      gitHubAppId: this.appId.trim() || undefined,
      gitHubAppPrivateKey: this.secretValue('appPrivateKey', this.appPrivateKey),
      gitHubAccessToken: this.secretValue('accessToken', this.accessToken),
      gitHubWebhookSecret: this.secretValue('webhookSecret', this.webhookSecret),
      workflowRunThreshold: this.workflowRunThreshold,
      enabled: this.integrationEnabled,
    };

    this.client.updateGitHubConfig(this.courseId, request).subscribe({
      next: () => {
        this.savingIntegration.set(false);
        this.appPrivateKey = '';
        this.accessToken = '';
        this.webhookSecret = '';
        this.clearing.set({});
        this.saved.set('GitHub integration saved.');
        this.load();
        this.runHealthCheck();
      },
      error: () => {
        this.savingIntegration.set(false);
        this.error.set('The GitHub integration could not be saved.');
      },
    });
  }

  /**
   * Translates a credential input into what the API expects: undefined keeps the stored value, an empty string
   * clears it, anything else replaces it.
   */
  private secretValue(field: string, typed: string): string | undefined {
    if (this.isClearing(field)) {
      return '';
    }
    return typed.trim() || undefined;
  }

  // ---- CI callback tokens ----

  protected createToken(): void {
    this.clearMessages();
    this.creatingToken.set(true);

    this.client.createToken(this.courseId, { description: this.newTokenDescription.trim() || undefined }).subscribe({
      next: (token) => {
        this.creatingToken.set(false);
        this.newTokenDescription = '';
        this.issuedToken.set(token);
        this.load();
        this.runHealthCheck();
      },
      error: () => {
        this.creatingToken.set(false);
        this.error.set('The token could not be created.');
      },
    });
  }

  protected revokeToken(token: WebhookTokenDto): void {
    this.clearMessages();
    this.client.revokeToken(this.courseId, token.id ?? 0).subscribe({
      next: () => {
        this.saved.set(`Token ${token.token} revoked. Evaluations signed with it will be rejected.`);
        this.load();
        this.runHealthCheck();
      },
      error: () => this.error.set('The token could not be revoked.'),
    });
  }

  protected dismissIssuedToken(): void {
    this.issuedToken.set(null);
  }

  /** Copies a token's secret to the clipboard, with brief per-row "Copied" feedback. */
  protected copySecret(token: WebhookTokenDto): void {
    const secret = token.secret;
    if (!secret) {
      return;
    }
    navigator.clipboard.writeText(secret).then(
      () => {
        this.copiedTokenId.set(token.id ?? null);
        setTimeout(() => {
          if (this.copiedTokenId() === token.id) {
            this.copiedTokenId.set(null);
          }
        }, 1500);
      },
      () => this.error.set('The secret could not be copied to the clipboard.'),
    );
  }

  /** Reveals or hides a token's secret inline, so it can be read as well as copied. */
  protected toggleReveal(token: WebhookTokenDto): void {
    const id = token.id ?? 0;
    const next = new Set(this.revealed());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.revealed.set(next);
  }

  // ---- Staff ----

  protected searchUsers(): void {
    const term = this.memberSearch.trim();
    if (term.length < 2) {
      this.candidates.set([]);
      return;
    }

    this.searching.set(true);
    this.usersClient.list(term, undefined, 0, 10).subscribe({
      next: (page) => {
        this.candidates.set(page.items ?? []);
        this.searching.set(false);
      },
      error: () => this.searching.set(false),
    });
  }

  protected addMember(user: UserDto, role: CourseRole): void {
    this.clearMessages();
    this.client.upsertMember(this.courseId, { userId: user.id ?? 0, role }).subscribe({
      next: () => {
        this.memberSearch = '';
        this.candidates.set([]);
        this.saved.set(`${user.userName} was added to this course.`);
        this.load();
      },
      error: () => this.error.set('That user could not be added.'),
    });
  }

  protected changeMemberRole(member: CourseMemberDto, role: string): void {
    this.clearMessages();
    this.client.upsertMember(this.courseId, { userId: member.userId ?? 0, role: role as CourseRole }).subscribe({
      next: () => this.load(),
      error: () => this.error.set('That role could not be changed.'),
    });
  }

  protected removeMember(member: CourseMemberDto): void {
    this.clearMessages();
    this.client.removeMember(this.courseId, member.userId ?? 0).subscribe({
      next: () => {
        this.saved.set(`${member.userName} was removed from this course.`);
        this.load();
      },
      error: () => this.error.set('That member could not be removed.'),
    });
  }

  /** Members already on the course are filtered out of the picker, so adding twice is not offered. */
  protected isMember(user: UserDto): boolean {
    return (this.course()?.members ?? []).some((m) => m.userId === user.id);
  }

  // ---- Delete ----

  protected deleteCourse(): void {
    this.clearMessages();
    this.deleting.set(true);

    this.client.delete(this.courseId, this.confirmSlug.trim()).subscribe({
      next: () => {
        this.deleting.set(false);
        void this.router.navigate(['/admin/courses']);
      },
      error: () => {
        this.deleting.set(false);
        this.error.set('The course was not deleted. The slug must match exactly.');
      },
    });
  }

  private clearMessages(): void {
    this.error.set(null);
    this.saved.set(null);
  }
}
