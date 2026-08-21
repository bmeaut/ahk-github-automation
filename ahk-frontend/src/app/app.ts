import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ImpersonationBanner } from './shared/impersonation-banner/impersonation-banner';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ImpersonationBanner],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('ahk-frontend');
}
