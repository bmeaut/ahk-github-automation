import { Component, input } from '@angular/core';

import { HealthCheckResult, HealthStatus } from '../../api/api-client';

/**
 * A course's health drawn as the pipeline it describes: webhook in, credentials, callback out. Each link is
 * tinted by its check, so a glance says *which* part of the integration is broken rather than only that
 * something is. Set `detailed` to print each check's message and next step underneath.
 */
@Component({
  selector: 'app-health-chain',
  templateUrl: './health-chain.html',
  styleUrl: './health-chain.scss',
})
export class HealthChain {
  readonly checks = input.required<HealthCheckResult[]>();
  readonly detailed = input(false);

  /** Maps a status onto the shared status classes (ok / warn / bad / idle). */
  protected tone(status: HealthStatus | undefined): string {
    switch (status) {
      case 'Healthy':
        return 'ok';
      case 'Warning':
        return 'warn';
      case 'Failed':
        return 'bad';
      default:
        return 'idle';
    }
  }

  protected label(status: HealthStatus | undefined): string {
    switch (status) {
      case 'Healthy':
        return 'Passing';
      case 'Warning':
        return 'Needs attention';
      case 'Failed':
        return 'Failing';
      default:
        return 'Not set up';
    }
  }
}
