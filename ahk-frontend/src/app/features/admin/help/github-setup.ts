import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';

import { CopyButton } from '../../../shared/copy-button/copy-button';

/**
 * A read-only guide to registering the per-course GitHub App and finding the values the GitHub integration
 * form asks for. It is static prose — the authoritative copy lives in ahk-backend/docs/github-app.md and the
 * two must be kept in step.
 *
 * When opened from a course's integration card the organization and the slug are passed as ?org= and ?slug=,
 * which lets the page deep-link into that organization's settings and name the App the admin should register
 * instead of showing an example. Without them the guide still stands on its own — but it then offers no copy
 * button for the App name, because copying a placeholder into GitHub is worse than retyping the real thing.
 */
@Component({
  selector: 'app-github-setup-help',
  imports: [RouterLink, CopyButton],
  templateUrl: './github-setup.html',
  styleUrl: './github-setup.scss',
})
export class GitHubSetupHelp {
  private readonly route = inject(ActivatedRoute);

  /** Values that are the same for every course, kept here so the prose and its copy button cannot drift. */
  protected readonly homepageUrl = 'https://ahk.aut.bme.hu';
  protected readonly webhookUrl = 'https://ahk.aut.bme.hu/api/integrations/github';

  /** The organization to deep-link into, when the page was opened from a specific course. */
  protected readonly org = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('org')?.trim() || null)),
    { initialValue: null },
  );

  /** That course's slug, which is what makes the suggested App name unique across GitHub. */
  protected readonly slug = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('slug')?.trim() || null)),
    { initialValue: null },
  );

  /** The exact name to register the App under, or null when the page was opened without a course. */
  protected readonly appName = computed(() => {
    const slug = this.slug();
    return slug ? `AHK Monitor (AUT) - ${slug.toUpperCase()}` : null;
  });
}
