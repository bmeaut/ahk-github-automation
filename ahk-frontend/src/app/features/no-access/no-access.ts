import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

/** Landing for a signed-in user who is not assigned to any course yet, so the guards have nowhere to send them. */
@Component({
  selector: 'app-no-access',
  templateUrl: './no-access.html',
  styleUrl: './no-access.scss',
})
export class NoAccess {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.currentUser;

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
