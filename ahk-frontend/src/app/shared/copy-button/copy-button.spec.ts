import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { CopyButton } from './copy-button';

describe('CopyButton', () => {
  let fixture: ComponentFixture<CopyButton>;
  let writeText: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });

    await TestBed.configureTestingModule({ imports: [CopyButton] }).compileComponents();

    fixture = TestBed.createComponent(CopyButton);
    fixture.componentRef.setInput('value', 'AHK_APPTOKEN');
    fixture.componentRef.setInput('label', 'token');
    fixture.detectChanges();
  });

  function button(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button');
  }

  it('labels itself with the value it copies, since it has no caption', () => {
    expect(button().getAttribute('aria-label')).toBe('Copy token');
    expect(button().textContent?.trim()).toBe('');
  });

  it('copies the value and says so', async () => {
    button().click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith('AHK_APPTOKEN');
    expect(button().classList).toContain('copied');
    expect(button().getAttribute('title')).toBe('Copied');
  });

  it('reports a refused copy rather than claiming success', async () => {
    writeText.mockRejectedValue(new Error('denied'));
    // The helper falls back to execCommand, which jsdom does not implement either.
    Object.defineProperty(document, 'execCommand', { value: () => false, configurable: true });

    button().click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(button().classList).toContain('failed');
  });

  it('does nothing when there is no value to copy', async () => {
    fixture.componentRef.setInput('value', null);
    fixture.detectChanges();

    expect(button().disabled).toBe(true);
  });
});
