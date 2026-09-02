import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  AssignmentAcceptanceDto,
  AssignmentDto,
  AssignmentsClient,
  SaveAssignmentRequest,
  TemplateCheckDto,
} from '../../../api/api-client';
import { readApiError } from '../../../core/api-error';
import { copyToClipboard } from '../../../core/clipboard';
import { CourseContextService } from '../../../core/course/course-context.service';
import { CourseTally } from '../../../shared/course-tally/course-tally';

/**
 * Assignment administration for a course — what used to be set up in GitHub Classroom, and the course's
 * landing page, which is why the course-wide tally sits at the top of it.
 *
 * The invite link is the point of this screen, so copying it is a first-class action rather than something
 * the instructor has to select out of a table cell.
 */
@Component({
  selector: 'app-course-assignments',
  imports: [FormsModule, DatePipe, RouterLink, CourseTally],
  templateUrl: './assignments.html',
  styleUrl: './assignments.scss',
})
export class CourseAssignments {
  private readonly client = inject(AssignmentsClient);
  private readonly courseContext = inject(CourseContextService);

  protected readonly assignments = signal<AssignmentDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly saved = signal<string | null>(null);
  protected readonly showArchived = signal(false);

  /** Which invite link was copied last, so only that row shows the confirmation. */
  protected readonly copiedId = signal<number | null>(null);

  // ---- Editor ----
  protected readonly editingId = signal<number | null>(null);
  protected readonly creating = signal(false);
  /**
   * The saved assignment the editor is open on, or null while creating. The editor carries everything the list
   * does not — invite link, dates, archive, delete — and all of that needs the stored row, not the form fields.
   * Reading it from the list means a reload after archiving or reissuing a link refreshes the editor too.
   */
  protected readonly editingAssignment = computed(() => {
    const id = this.editingId();
    return id === null ? null : (this.assignments().find((a) => a.id === id) ?? null);
  });
  protected readonly savingForm = signal(false);
  protected name = '';
  protected description = '';
  protected templateRepoName = '';
  protected repoNamePrefix = '';

  /** Advisory check of the template repository; costs a GitHub call, so it is only run on demand. */
  protected readonly template = signal<TemplateCheckDto | null>(null);
  protected readonly checkingTemplate = signal(false);
  /** After a save, the template problem (if any) for the just-saved assignment, shown above the list. */
  protected readonly templateWarning = signal<string | null>(null);

  // ---- Acceptance roster ----
  protected readonly expandedId = signal<number | null>(null);
  protected readonly acceptances = signal<AssignmentAcceptanceDto[]>([]);
  protected readonly loadingAcceptances = signal(false);

  /** Protected, not private: the submissions link in the template is built from it. */
  protected get course(): string {
    return this.courseContext.activeSlug() ?? '';
  }

  constructor() {
    // The course switcher navigates between sibling /{course}/assignments routes, which reuses this component
    // instance — ngOnInit would fire only for the first course. Reloading off the slug signal keeps the list
    // with the header. The open editor and the expanded roster hold ids from the course being left, so they
    // are closed rather than carried over.
    effect(() => {
      const slug = this.courseContext.activeSlug();
      if (slug) {
        untracked(() => {
          this.cancelEdit();
          this.expandedId.set(null);
          this.acceptances.set([]);
          this.clearMessages();
          this.load();
        });
      }
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.client.list(this.course, this.showArchived()).subscribe({
      next: (assignments) => {
        this.assignments.set(assignments);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readApiError(err, 'The assignments could not be loaded.'));
        this.loading.set(false);
      },
    });
  }

  protected toggleArchived(): void {
    this.showArchived.update((v) => !v);
    this.load();
  }

  // ---- Editor ----

  protected startCreate(): void {
    this.clearMessages();
    this.creating.set(true);
    this.editingId.set(null);
    this.template.set(null);
    this.name = '';
    this.description = '';
    this.templateRepoName = '';
    this.repoNamePrefix = '';
  }

  protected startEdit(assignment: AssignmentDto): void {
    this.clearMessages();
    this.creating.set(false);
    this.editingId.set(assignment.id ?? null);
    this.template.set(null);
    this.name = assignment.name ?? '';
    this.description = assignment.description ?? '';
    this.templateRepoName = assignment.templateRepoName ?? '';
    this.repoNamePrefix = assignment.repoNamePrefix ?? '';
  }

  protected cancelEdit(): void {
    this.creating.set(false);
    this.editingId.set(null);
    this.template.set(null);
  }

  protected save(): void {
    this.clearMessages();
    this.savingForm.set(true);

    const request: SaveAssignmentRequest = {
      name: this.name.trim(),
      description: this.description.trim() || undefined,
      templateRepoName: this.templateRepoName.trim(),
      repoNamePrefix: this.repoNamePrefix.trim() || undefined,
    };

    const id = this.editingId();
    const call = id === null ? this.client.create(request, this.course) : this.client.update(id, request, this.course);

    call.subscribe({
      next: (assignment) => {
        this.savingForm.set(false);
        this.saved.set(
          id === null
            ? `"${assignment.name}" was created. Copy its invite link and share it with your students.`
            : `"${assignment.name}" was saved.`,
        );
        this.cancelEdit();
        this.load();
        // Re-verify the template now that it is saved, so a tester who never opens the check still sees a
        // problem before students hit it at accept time. Advisory: it never affects whether the save succeeded.
        this.verifySavedTemplate(assignment.name ?? '', request.templateRepoName ?? '');
      },
      error: (err: unknown) => {
        this.savingForm.set(false);
        this.error.set(readApiError(err, 'The assignment could not be saved.'));
      },
    });
  }

  /**
   * Asks GitHub whether the template repository exists and is marked as a template. Advisory only — an
   * assignment may legitimately be drafted before its template does. Works in both create and edit mode
   * because it checks the name currently in the field, not a stored assignment.
   */
  protected checkTemplate(): void {
    const repo = this.templateRepoName.trim();
    if (!repo) {
      return;
    }

    this.checkingTemplate.set(true);
    this.client.checkTemplate({ templateRepoName: repo }, this.course).subscribe({
      next: (result) => {
        this.template.set(result);
        this.checkingTemplate.set(false);
      },
      error: () => this.checkingTemplate.set(false),
    });
  }

  /** Runs the template check for a just-saved assignment and, if there is a problem, surfaces it above the list. */
  private verifySavedTemplate(assignmentName: string, templateRepoName: string): void {
    this.templateWarning.set(null);
    if (!templateRepoName) {
      return;
    }

    this.client.checkTemplate({ templateRepoName }, this.course).subscribe({
      next: (result) => {
        if (result.problem) {
          this.templateWarning.set(`"${assignmentName}": ${result.problem}`);
        }
      },
      // A failed advisory check must never look like a failed save; stay silent.
      error: () => {},
    });
  }

  // ---- Invite link ----

  /**
   * The API returns the invite link as a path, not an absolute URL — behind the dev proxy it would otherwise
   * be built from a rewritten Host header and point at the backend's port. The browser's own origin is by
   * definition the one the student will use.
   */
  protected inviteUrl(assignment: AssignmentDto): string {
    return `${window.location.origin}${assignment.invitePath ?? ''}`;
  }

  protected async copyInvite(assignment: AssignmentDto): Promise<void> {
    this.clearMessages();

    const copied = await copyToClipboard(this.inviteUrl(assignment));
    if (copied) {
      this.copiedId.set(assignment.id ?? null);
      setTimeout(() => this.copiedId.set(null), 2500);
    } else {
      this.error.set('The link could not be copied automatically — select it and copy it by hand.');
    }
  }

  /** Retires the current link. Anyone holding the old one gets "this invite link does not match an assignment". */
  protected regenerateInvite(assignment: AssignmentDto): void {
    this.clearMessages();
    this.client.regenerateInvite(assignment.id ?? 0, this.course).subscribe({
      next: () => {
        this.saved.set(
          `"${assignment.name}" has a new invite link. The previous one no longer works — share the new one.`,
        );
        this.load();
      },
      error: (err: unknown) => this.error.set(readApiError(err, 'A new invite link could not be issued.')),
    });
  }

  // ---- Lifecycle ----

  protected setArchived(assignment: AssignmentDto, archived: boolean): void {
    this.clearMessages();

    const id = assignment.id ?? 0;
    const call = archived ? this.client.archive(id, this.course) : this.client.unarchive(id, this.course);

    call.subscribe({
      next: () => {
        this.saved.set(
          archived
            ? `"${assignment.name}" was archived. Its invite link no longer accepts new students; the ones who already accepted keep their repositories.`
            : `"${assignment.name}" was reopened and accepts students again.`,
        );
        this.load();
      },
      error: (err: unknown) => this.error.set(readApiError(err, 'That could not be changed.')),
    });
  }

  protected remove(assignment: AssignmentDto): void {
    this.clearMessages();
    this.client.delete(assignment.id ?? 0, this.course).subscribe({
      next: () => {
        this.saved.set(`"${assignment.name}" was deleted.`);
        // Delete is offered from the editor, so the editor has to close with it — there is nothing left to edit.
        this.cancelEdit();
        this.load();
      },
      error: (err: unknown) => this.error.set(readApiError(err, 'That assignment could not be deleted.')),
    });
  }

  // ---- Acceptances ----

  protected toggleAcceptances(assignment: AssignmentDto): void {
    const id = assignment.id ?? 0;

    if (this.expandedId() === id) {
      this.expandedId.set(null);
      return;
    }

    this.expandedId.set(id);
    this.acceptances.set([]);
    this.loadingAcceptances.set(true);

    this.client.listAcceptances(id, this.course).subscribe({
      next: (acceptances) => {
        this.acceptances.set(acceptances);
        this.loadingAcceptances.set(false);
      },
      error: () => this.loadingAcceptances.set(false),
    });
  }

  private clearMessages(): void {
    this.error.set(null);
    this.saved.set(null);
    this.templateWarning.set(null);
  }
}
