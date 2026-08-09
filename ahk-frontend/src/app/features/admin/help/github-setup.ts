import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';

/**
 * A read-only guide to registering the per-course GitHub App and finding the values the GitHub integration
 * form asks for. It is static prose — the authoritative copy lives in ahk-backend/docs/github-app.md and the
 * two must be kept in step.
 *
 * When opened from a course's integration card the organization is passed as ?org=, which lets the page render
 * a direct link into that organization's Developer settings. Without it the guide still stands on its own.
 */
@Component({
  selector: 'app-github-setup-help',
  imports: [RouterLink],
  templateUrl: './github-setup.html',
  styleUrl: './github-setup.scss',
})
export class GitHubSetupHelp {
  private readonly route = inject(ActivatedRoute);

  /** The organization to deep-link into, when the page was opened from a specific course. */
  protected readonly org = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('org')?.trim() || null)),
    { initialValue: null },
  );
}
