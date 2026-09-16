import { Component, input, signal } from '@angular/core';

import { copyToClipboard } from '../../core/clipboard';

/**
 * A clipboard icon that copies one value. Icon only — it sits beside a value that is already labelled, so a
 * word per button would only repeat what the line already says.
 *
 * It exists to stop the "keyed copied() signal + setTimeout reset" idiom from being hand-copied into a fourth
 * component. The trade is that it has no error channel: a failed copy says so in its tooltip rather than in the
 * host page's error banner, which is why the hosts that still want that banner keep their own copy() method.
 */
@Component({
  selector: 'app-copy-button',
  templateUrl: './copy-button.html',
  styleUrl: './copy-button.scss',
})
export class CopyButton {
  /** The text placed on the clipboard. */
  readonly value = input.required<string | undefined | null>();

  /** Names the value for screen readers and the tooltip, e.g. "webhook URL" → "Copy webhook URL". */
  readonly label = input('');

  /** null = idle, true = copied, false = the browser refused. Reverts to idle after {@link resetAfterMs}. */
  protected readonly state = signal<boolean | null>(null);

  private static readonly resetAfterMs = 2500;

  /** Cleared on every click, so a second copy restarts the window instead of being cut short by the first. */
  private reset?: ReturnType<typeof setTimeout>;

  protected title(): string {
    switch (this.state()) {
      case true:
        return 'Copied';
      case false:
        return 'Could not copy — select the value and copy it by hand';
      default:
        return this.label() ? `Copy ${this.label()}` : 'Copy to clipboard';
    }
  }

  protected async copy(): Promise<void> {
    const value = this.value();
    if (!value) {
      return;
    }

    this.state.set(await copyToClipboard(value));

    clearTimeout(this.reset);
    this.reset = setTimeout(() => this.state.set(null), CopyButton.resetAfterMs);
  }
}
